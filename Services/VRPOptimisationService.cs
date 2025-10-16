using Google.OrTools.ConstraintSolver;
using MyEcologicCrowsourcingApp.Models;
using System.Text.Json;

namespace MyEcologicCrowsourcingApp.Services
{
    public class VRPOptimisationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<VRPOptimisationService> _logger;
        private const string OSRM_BASE_URL = "http://localhost:5008"; // Votre OSRM local

        public VRPOptimisationService(
            IHttpClientFactory httpClientFactory,
            ILogger<VRPOptimisationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<List<Itineraire>> OptimiserTournees(
            List<PointDechet> pointsDechets,
            List<Vehicule> vehicules,
            Location depot)
        {
            _logger.LogInformation("Début optimisation: {Count} points, {Vehicles} véhicules",
                pointsDechets.Count, vehicules.Count);

            // ÉTAPE 1: Obtenir la matrice de distances réelles via OSRM
            var distanceMatrix = await GetDistanceMatrixFromOSRM(pointsDechets, depot);

            // ÉTAPE 2: Résoudre le VRP avec OR-Tools
            var solution = SolveVRPWithORTools(
                pointsDechets,
                vehicules,
                distanceMatrix,
                depot);

            // ÉTAPE 3: Enrichir avec les routes détaillées OSRM
            var itineraires = await EnrichWithOSRMRoutes(solution, pointsDechets, depot);

            _logger.LogInformation("Optimisation terminée: {Count} itinéraires créés", itineraires.Count);
            return itineraires;
        }

        private async Task<long[,]> GetDistanceMatrixFromOSRM(
            List<PointDechet> points,
            Location depot)
        {
            _logger.LogInformation("Calcul matrice de distances via OSRM...");

            // Construire la liste de coordonnées (depot + tous les points)
            var coordinates = new List<string>
            {
                $"{depot.Longitude},{depot.Latitude}" // Index 0 = dépôt
            };

            foreach (var point in points)
            {
                coordinates.Add($"{point.Longitude},{point.Latitude}");
            }

            // Appel OSRM Table API
            var coordinatesString = string.Join(";", coordinates);
            var url = $"{OSRM_BASE_URL}/table/v1/driving/{coordinatesString}?annotations=distance,duration";

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OSRM non disponible, utilisation distances Haversine");
                return CalculateHaversineMatrix(points, depot);
            }

            var json = await response.Content.ReadAsStringAsync();
            var osrmResponse = JsonSerializer.Deserialize<OSRMTableResponse>(json);

            if (osrmResponse?.distances == null)
            {
                throw new InvalidOperationException("La réponse OSRM est invalide ou ne contient pas de distances.");
            }

            // Convertir en matrice OR-Tools (distances en mètres)
            int size = points.Count + 1;
            var matrix = new long[size, size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    matrix[i, j] = (long)osrmResponse.distances[i][j];
                }
            }

            _logger.LogInformation("Matrice de distances calculée: {Size}x{Size}", size, size);
            return matrix;
        }

        // calcule la distance la plus courte entre deux points sur une sphère 
        private long[,] CalculateHaversineMatrix(List<PointDechet> points, Location depot)
        {
            int size = points.Count + 1;
            var matrix = new long[size, size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (i == j)
                    {
                        matrix[i, j] = 0;
                        continue;
                    }

                    var lat1 = i == 0 ? depot.Latitude : points[i - 1].Latitude;
                    var lon1 = i == 0 ? depot.Longitude : points[i - 1].Longitude;
                    var lat2 = j == 0 ? depot.Latitude : points[j - 1].Latitude;
                    var lon2 = j == 0 ? depot.Longitude : points[j - 1].Longitude;

                    matrix[i, j] = (long)(HaversineDistance(lat1, lon1, lat2, lon2) * 1000); // en mètres
                }
            }

            return matrix;
        }

        private double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;

        private VRPSolution SolveVRPWithORTools(
            List<PointDechet> points,
            List<Vehicule> vehicules,
            long[,] distanceMatrix,
            Location depot)
        {
            _logger.LogInformation("Résolution VRP avec OR-Tools...");

            int numLocations = points.Count + 1; // +1 pour le dépôt
            int numVehicles = vehicules.Count;
            int depotIndex = 0;

            // Créer le RoutingIndexManager
            // PARAMÈTRE: nombre de locations, nombre de véhicules, index du dépôt
            RoutingIndexManager manager = new RoutingIndexManager(
                numLocations,
                numVehicles,
                depotIndex);

            // Créer le modèle de routage
            RoutingModel routing = new RoutingModel(manager);

            // CALLBACK DE DISTANCE
            // Définit comment calculer la distance entre deux points
            int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
            {
                var fromNode = manager.IndexToNode(fromIndex);
                var toNode = manager.IndexToNode(toIndex);
                return distanceMatrix[fromNode, toNode];
            });

            // Définir le coût de l'arc (distance)
            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            // CONTRAINTE DE CAPACITÉ
            // Chaque véhicule a une capacité maximale (en m³)
            int demandCallbackIndex = routing.RegisterUnaryTransitCallback((long fromIndex) =>
            {
                var fromNode = manager.IndexToNode(fromIndex);
                if (fromNode == 0) return 0; // Dépôt = pas de demande
                return (long)((points[fromNode - 1].VolumeEstime ?? 1.0) * 1000); // Convertir en litres
            });

            routing.AddDimensionWithVehicleCapacity(
                demandCallbackIndex,
                0, // slack (pas de marge)
                vehicules.Select(v => (long)(v.CapaciteMax * 1000)).ToArray(), // Capacités en litres
                true, // start_cumul_to_zero
                "Capacity");

            // CONTRAINTE DE DISTANCE MAXIMALE (optionnel)
            // Limite la distance totale par véhicule
            routing.AddDimension(
                transitCallbackIndex,
                0, // pas de temps d'attente
                100000000, // distance max par véhicule (100 000 km en mètres)
                true,
                "Distance");

            // PARAMÈTRES DE RECHERCHE
            RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();

            // STRATÉGIE DE PREMIÈRE SOLUTION
            // PATH_CHEAPEST_ARC = commence par les arcs les moins coûteux
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;

            // MÉTAHEURISTIQUE DE RECHERCHE LOCALE
            // GUIDED_LOCAL_SEARCH = algorithme performant pour améliorer la solution
            searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;

            // LIMITE DE TEMPS (5 secondes)
            searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 5 };

            _logger.LogInformation("Lancement recherche de solution...");

            // RÉSOUDRE
            Assignment solution = routing.SolveWithParameters(searchParameters);

            if (solution == null)
            {
                _logger.LogError("Aucune solution trouvée!");
                throw new Exception("Impossible de trouver une solution VRP");
            }

            _logger.LogInformation("Solution trouvée! Coût total: {Cost} mètres", solution.ObjectiveValue());

            // EXTRAIRE LA SOLUTION
            var vrpSolution = new VRPSolution
            {
                TotalDistance = solution.ObjectiveValue(),
                Routes = new List<VehicleRoute>()
            };

            for (int vehicleId = 0; vehicleId < numVehicles; vehicleId++)
            {
                var route = new VehicleRoute
                {
                    VehicleId = vehicleId,
                    PointIndices = new List<int>(),
                    Distance = 0
                };

                long index = routing.Start(vehicleId);

                while (!routing.IsEnd(index))
                {
                    int nodeIndex = manager.IndexToNode(index);
                    route.PointIndices.Add(nodeIndex);

                    long previousIndex = index;
                    index = solution.Value(routing.NextVar(index));
                    route.Distance += routing.GetArcCostForVehicle(previousIndex, index, vehicleId);
                }

                // Ajouter le retour au dépôt
                route.PointIndices.Add(manager.IndexToNode(index));

                // N'ajouter que les routes non vides
                if (route.PointIndices.Count > 2) // Plus que dépôt départ et retour
                {
                    vrpSolution.Routes.Add(route);
                }
            }

            return vrpSolution;
        }

        private async Task<List<Itineraire>> EnrichWithOSRMRoutes(
            VRPSolution solution,
            List<PointDechet> points,
            Location depot)
        {
            _logger.LogInformation("Enrichissement avec routes OSRM détaillées...");

            var itineraires = new List<Itineraire>();
            var client = _httpClientFactory.CreateClient();

            foreach (var route in solution.Routes)
            {
                var itineraire = new Itineraire
                {
                    Id = Guid.NewGuid(),
                    ListePoints = new List<PointDechet>(),
                    DistanceTotale = 0,
                    DureeEstimee = TimeSpan.Zero,
                    CarburantEstime = 0
                };

                // Construire la liste des coordonnées pour cette route
                var coordinates = new List<string>();

                foreach (var nodeIndex in route.PointIndices)
                {
                    if (nodeIndex == 0)
                    {
                        coordinates.Add($"{depot.Longitude},{depot.Latitude}");
                    }
                    else
                    {
                        var point = points[nodeIndex - 1];
                        coordinates.Add($"{point.Longitude},{point.Latitude}");
                        itineraire.ListePoints.Add(point);
                    }
                }

                // Obtenir la route détaillée via OSRM
                var coordinatesString = string.Join(";", coordinates);
                var url = $"{OSRM_BASE_URL}/route/v1/driving/{coordinatesString}?overview=full&geometries=geojson&steps=true";

                try
                {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var osrmRoute = JsonSerializer.Deserialize<OSRMRouteResponse>(json);

                        if (osrmRoute?.routes?.Length > 0)
                        {
                            var routeData = osrmRoute.routes[0];
                            itineraire.DistanceTotale = routeData.distance / 1000.0; // Convertir en km
                            itineraire.DureeEstimee = TimeSpan.FromSeconds(routeData.duration);

                            // Estimer carburant (8L/100km pour camion moyen)
                            itineraire.CarburantEstime = (itineraire.DistanceTotale / 100.0) * 8.0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erreur récupération route OSRM, utilisation estimation");
                    itineraire.DistanceTotale = route.Distance / 1000.0; // Distance en km
                    itineraire.DureeEstimee = TimeSpan.FromMinutes(itineraire.DistanceTotale * 2); // ~30 km/h moyenne
                }

                itineraires.Add(itineraire);
            }

            return itineraires;
        }
    }
}