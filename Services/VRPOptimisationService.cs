using Google.OrTools.ConstraintSolver;
using MyEcologicCrowsourcingApp.Models;
using System.Text.Json;
using System.Globalization;

namespace MyEcologicCrowsourcingApp.Services
{
    public class VRPOptimisationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<VRPOptimisationService> _logger;
        private const string OSRM_BASE_URL = "https://router.project-osrm.org";

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

            // VÉRIFIER LA FAISABILITÉ
            var volumeTotal = pointsDechets.Sum(p => p.VolumeEstime ?? 5.0);
            var capaciteTotal = vehicules.Sum(v => v.CapaciteMax);
            
            _logger.LogInformation("Volume total: {Volume} m³, Capacité totale: {Capacite} m³", 
                volumeTotal, capaciteTotal);

            // Si capacité insuffisante, créer des véhicules virtuels (multi-tournées)
            var vehiculesOptimises = vehicules.ToList();
            if (volumeTotal > capaciteTotal)
            {
                _logger.LogWarning("Capacité insuffisante! Activation du mode multi-tournées");
                vehiculesOptimises = CreerVehiculesVirtuels(vehicules, volumeTotal);
            }

            // ÉTAPE 1: Obtenir la matrice de distances réelles via OSRM
            var distanceMatrix = await GetDistanceMatrixFromOSRM(pointsDechets, depot);

            // ÉTAPE 2: Résoudre le VRP avec OR-Tools
            var solution = SolveVRPWithORTools(
                pointsDechets,
                vehiculesOptimises,
                distanceMatrix,
                depot);

            // ÉTAPE 3: Enrichir avec les routes détaillées OSRM
            var itineraires = await EnrichWithOSRMRoutes(solution, pointsDechets, depot, vehicules);

            _logger.LogInformation("Optimisation terminée: {Count} itinéraires créés", itineraires.Count);
            return itineraires;
        }

        /// <summary>
        /// Crée des véhicules virtuels pour permettre les multi-tournées
        /// </summary>
        private List<Vehicule> CreerVehiculesVirtuels(List<Vehicule> vehiculesReels, double volumeTotal)
        {
            var vehiculesVirtuels = new List<Vehicule>();
            
            foreach (var vehicule in vehiculesReels)
            {
                // Calculer combien de tournées sont nécessaires
                int nombreTournees = (int)Math.Ceiling(volumeTotal / vehicule.CapaciteMax);
                
                _logger.LogInformation("Véhicule {Immat}: {NbTournees} tournées nécessaires", 
                    vehicule.Immatriculation, nombreTournees);

                // Créer un véhicule virtuel par tournée
                for (int i = 0; i < nombreTournees; i++)
                {
                    vehiculesVirtuels.Add(new Vehicule
                    {
                        Id = vehicule.Id,
                        Immatriculation = $"{vehicule.Immatriculation} (Tournée {i + 1})",
                        Type = vehicule.Type,
                        CapaciteMax = vehicule.CapaciteMax,
                        EstDisponible = true,
                        OrganisationId = vehicule.OrganisationId
                    });
                }
            }

            _logger.LogInformation("Véhicules virtuels créés: {Count}", vehiculesVirtuels.Count);
            return vehiculesVirtuels;
        }

        private async Task<long[,]> GetDistanceMatrixFromOSRM(
            List<PointDechet> points,
            Location depot)
        {
            _logger.LogInformation("Calcul matrice de distances via OSRM...");

            var coordinates = new List<string>
            {
                $"{depot.Longitude.ToString("F6", CultureInfo.InvariantCulture)},{depot.Latitude.ToString("F6", CultureInfo.InvariantCulture)}"
            };

            foreach (var point in points)
            {
                coordinates.Add($"{point.Longitude.ToString("F6", CultureInfo.InvariantCulture)},{point.Latitude.ToString("F6", CultureInfo.InvariantCulture)}");
            }

            var coordinatesString = string.Join(";", coordinates);
            var url = $"{OSRM_BASE_URL}/table/v1/driving/{coordinatesString}?annotations=distance,duration";

            _logger.LogInformation("URL OSRM: {Url}", url);

            var client = _httpClientFactory.CreateClient();
            
            try
            {
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OSRM non disponible (Code: {StatusCode}), utilisation distances Haversine", response.StatusCode);
                    return CalculateHaversineMatrix(points, depot);
                }

                var json = await response.Content.ReadAsStringAsync();
                var osrmResponse = JsonSerializer.Deserialize<OSRMTableResponse>(json);

                if (osrmResponse?.distances == null)
                {
                    _logger.LogWarning("Réponse OSRM invalide, utilisation distances Haversine");
                    return CalculateHaversineMatrix(points, depot);
                }

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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erreur OSRM, utilisation distances Haversine");
                return CalculateHaversineMatrix(points, depot);
            }
        }

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

                    matrix[i, j] = (long)(HaversineDistance(lat1, lon1, lat2, lon2) * 1000);
                }
            }

            return matrix;
        }

        private double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
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

            int numLocations = points.Count + 1;
            int numVehicles = vehicules.Count;
            int depotIndex = 0;

            RoutingIndexManager manager = new RoutingIndexManager(
                numLocations,
                numVehicles,
                depotIndex);

            RoutingModel routing = new RoutingModel(manager);

            int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
            {
                var fromNode = manager.IndexToNode(fromIndex);
                var toNode = manager.IndexToNode(toIndex);
                return distanceMatrix[fromNode, toNode];
            });

            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            int demandCallbackIndex = routing.RegisterUnaryTransitCallback((long fromIndex) =>
            {
                var fromNode = manager.IndexToNode(fromIndex);
                if (fromNode == 0) return 0;
                
                var volume = points[fromNode - 1].VolumeEstime ?? 5.0;
                return (long)(volume * 1000);
            });

            // Capacités réelles (pas augmentées pour forcer la solution)
            var capacites = vehicules.Select(v => (long)(v.CapaciteMax * 1000)).ToArray();

            routing.AddDimensionWithVehicleCapacity(
                demandCallbackIndex,
                0,
                capacites,
                true,
                "Capacity");

            routing.AddDimension(
                transitCallbackIndex,
                0,
                200000000,
                true,
                "Distance");

            RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
            
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
            searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
            searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 30 };

            _logger.LogInformation("Lancement recherche de solution... ({NumPoints} points, {NumVehicles} véhicules)", 
                points.Count, numVehicles);

            Assignment solution = routing.SolveWithParameters(searchParameters);

            if (solution == null)
            {
                var volumeTotal = points.Sum(p => p.VolumeEstime ?? 5.0);
                var capaciteTotal = vehicules.Sum(v => v.CapaciteMax);
                
                _logger.LogError("Aucune solution trouvée!");
                _logger.LogError("Volume total demandes: {VolumeTotal} m³, Capacité totale: {CapaciteTotal} m³", 
                    volumeTotal, capaciteTotal);
                
                throw new Exception($"Impossible de trouver une solution VRP. Volume demandé ({volumeTotal:F2} m³) vs Capacité disponible ({capaciteTotal:F2} m³). Ajoutez plus de véhicules ou activez le mode multi-tournées.");
            }

            _logger.LogInformation("Solution trouvée! Coût total: {Cost} mètres", solution.ObjectiveValue());

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

                route.PointIndices.Add(manager.IndexToNode(index));

                vrpSolution.Routes.Add(route);
                
                _logger.LogInformation("Route véhicule {VehicleId}: {PointCount} points, {Distance} m", 
                    vehicleId, route.PointIndices.Count - 2, route.Distance);
            }

            return vrpSolution;
        }

        private async Task<List<Itineraire>> EnrichWithOSRMRoutes(
            VRPSolution solution,
            List<PointDechet> points,
            Location depot,
            List<Vehicule> vehiculesReels)
        {
            _logger.LogInformation("Enrichissement avec routes OSRM détaillées...");

            var itineraires = new List<Itineraire>();
            var client = _httpClientFactory.CreateClient();

            // Grouper les routes par véhicule réel
            var routesParVehicule = solution.Routes
                .Select((route, index) => new { route, index })
                .GroupBy(x => x.index % vehiculesReels.Count)
                .ToList();

            int numeroTournee = 1;

            foreach (var groupe in routesParVehicule)
            {
                var vehiculeReel = vehiculesReels[groupe.Key];

                foreach (var routeInfo in groupe)
                {
                    var route = routeInfo.route;
                    
                    // Ignorer les routes vides
                    if (route.PointIndices.Count <= 2) continue;

                    var itineraire = new Itineraire
                    {
                        Id = Guid.NewGuid(),
                        ListePoints = new List<PointDechet>(),
                        DistanceTotale = 0,
                        DureeEstimee = TimeSpan.Zero,
                        CarburantEstime = 0
                    };

                    var coordinates = new List<string>();

                    foreach (var nodeIndex in route.PointIndices)
                    {
                        if (nodeIndex == 0)
                        {
                            coordinates.Add($"{depot.Longitude.ToString("F6", CultureInfo.InvariantCulture)},{depot.Latitude.ToString("F6", CultureInfo.InvariantCulture)}");
                        }
                        else
                        {
                            var point = points[nodeIndex - 1];
                            coordinates.Add($"{point.Longitude.ToString("F6", CultureInfo.InvariantCulture)},{point.Latitude.ToString("F6", CultureInfo.InvariantCulture)}");
                            itineraire.ListePoints.Add(point);
                        }
                    }

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
                                itineraire.DistanceTotale = routeData.distance / 1000.0;
                                itineraire.DureeEstimee = TimeSpan.FromSeconds(routeData.duration);
                                itineraire.CarburantEstime = (itineraire.DistanceTotale / 100.0) * 8.0;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erreur récupération route OSRM, utilisation estimation");
                        itineraire.DistanceTotale = route.Distance / 1000.0;
                        itineraire.DureeEstimee = TimeSpan.FromMinutes(itineraire.DistanceTotale * 2);
                    }

                    itineraires.Add(itineraire);
                    numeroTournee++;
                }
            }

            return itineraires;
        }
    }
}