using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Data;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Services;
using MyEcologicCrowsourcingApp.DTOs;
using System.Globalization;

namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OptimisationController : ControllerBase
    {
        private readonly EcologicDbContext _context;
        private readonly VRPOptimisationService _vrpService;
        private readonly ILogger<OptimisationController> _logger;

        public OptimisationController(
            EcologicDbContext context,
            VRPOptimisationService vrpService,
            ILogger<OptimisationController> logger)
        {
            _context = context;
            _vrpService = vrpService;
            _logger = logger;
        }

        [HttpPost("optimiser")]
        public async Task<IActionResult> OptimiserTournees([FromBody] OptimisationRequestDto request)
        {
            try
            {
                _logger.LogInformation("Début optimisation pour organisation {OrgId}", request.OrganisationId);

                // 1. RÉCUPÉRER L'ORGANISATION AVEC SES RELATIONS
                var organisation = await _context.Set<Organisation>()
                    .Include(o => o.Vehicules)
                    .Include(o => o.Depots)
                    .FirstOrDefaultAsync(o => o.OrganisationId == request.OrganisationId);

                if (organisation == null)
                    return NotFound(new { message = "Organisation non trouvée" });

                // 2. RÉCUPÉRER LES VÉHICULES DISPONIBLES
                List<Vehicule> vehicules;
                
                if (request.VehiculesIds?.Any() == true)
                {
                    // Option : Véhicules spécifiques sélectionnés
                    vehicules = await _context.Set<Vehicule>()
                        .Where(v => request.VehiculesIds.Contains(v.Id))
                        .Where(v => v.OrganisationId == request.OrganisationId)
                        .Where(v => v.EstDisponible)
                        .ToListAsync();

                    if (!vehicules.Any())
                        return BadRequest(new { message = "Aucun véhicule disponible parmi ceux sélectionnés" });
                }
                else
                {
                    // Prendre TOUS les véhicules disponibles de l'organisation
                    vehicules = await _context.Set<Vehicule>()
                        .Where(v => v.OrganisationId == request.OrganisationId)
                        .Where(v => v.EstDisponible)
                        .ToListAsync();

                    if (!vehicules.Any())
                        return BadRequest(new { message = "Aucun véhicule disponible pour cette organisation" });
                }

                _logger.LogInformation("Véhicules sélectionnés: {Count}", vehicules.Count);

                // 3. RÉCUPÉRER LE DÉPÔT AUTOMATIQUEMENT
                Depot depotEntity;
                
                if (request.DepotId.HasValue)
                {
                    // Option : Dépôt spécifique sélectionné
                    depotEntity = await _context.Set<Depot>()
                        .FirstOrDefaultAsync(d => d.Id == request.DepotId.Value 
                                                && d.OrganisationId == request.OrganisationId
                                                && d.EstActif);

                    if (depotEntity == null)
                        return BadRequest(new { message = "Dépôt introuvable ou inactif" });
                }
                else
                {
                    // Prendre le premier dépôt actif de l'organisation
                    depotEntity = await _context.Set<Depot>()
                        .FirstOrDefaultAsync(d => d.OrganisationId == request.OrganisationId 
                                                && d.EstActif);

                    if (depotEntity == null)
                    {
                        return BadRequest(new { 
                            message = "Aucun dépôt actif configuré pour cette organisation",
                            info = "Veuillez créer un dépôt avant de lancer l'optimisation"
                        });
                    }
                }

                var depot = new Location
                {
                    Latitude = depotEntity.Latitude,
                    Longitude = depotEntity.Longitude,
                    Name = depotEntity.Nom
                };

                _logger.LogInformation("Dépôt: {Name} ({Lat}, {Lon})", depot.Name, depot.Latitude, depot.Longitude);

                // 4. DÉTERMINER LA ZONE GÉOGRAPHIQUE VIA REVERSE GEOCODING
                string zoneGeographique = await DeterminerZoneGeographiqueAsync(depotEntity.Latitude, depotEntity.Longitude);
                
                _logger.LogInformation("Zone géographique déterminée: {Zone}", zoneGeographique);

                // 5. RÉCUPÉRER LES POINTS DE DÉCHETS DANS LA ZONE
                var pointsDechets = await _context.PointDechets
                    .Where(p => p.Statut == StatutDechet.Signale)
                    .Where(p => p.Zone.ToLower() == zoneGeographique.ToLower() || p.Zone == "Inconnue")
                    .ToListAsync();

                // Si aucun point dans la zone exacte, prendre les points dans un rayon de 50 km
                if (!pointsDechets.Any())
                {
                    _logger.LogWarning("Aucun point dans la zone {Zone}, recherche élargie dans un rayon de 50 km", zoneGeographique);
                    
                    var tousLesPoints = await _context.PointDechets
                        .Where(p => p.Statut == StatutDechet.Signale)
                        .ToListAsync();

                    // Filtrer par distance (rayon de 50 km)
                    pointsDechets = tousLesPoints
                        .Where(p => CalculerDistance(depotEntity.Latitude, depotEntity.Longitude, 
                                                    p.Latitude, p.Longitude) <= 50)
                        .ToList();
                }

                if (!pointsDechets.Any())
                {
                    return BadRequest(new { 
                        message = "Aucun point de déchet à collecter dans un rayon de 50 km du dépôt",
                        info = "Tous les déchets ont été nettoyés ou aucun déchet signalé dans cette zone",
                        depotZone = zoneGeographique,
                        depotCoordonnees = new { 
                            latitude = depotEntity.Latitude, 
                            longitude = depotEntity.Longitude 
                        }
                    });
                }

                _logger.LogInformation("Points disponibles: {Count} dans la zone {Zone}", 
                    pointsDechets.Count, zoneGeographique);

                // 6. OPTIMISER
                var itineraires = await _vrpService.OptimiserTournees(
                    pointsDechets,
                    vehicules,
                    depot);

                // 7. SAUVEGARDER
                var optimisationRequest = new OptimisationRequest
                {
                    Id = Guid.NewGuid(),
                    OrganisationId = request.OrganisationId,
                    ListePointsIds = pointsDechets.Select(p => p.Id).ToList(),
                    CapaciteVehicule = vehicules.Average(v => v.CapaciteMax),
                    TempsMaxParTrajet = request.TempsMaxParTrajet?? TimeSpan.FromHours(4),
                    ZoneGeographique = zoneGeographique
                };

                _context.Set<OptimisationRequest>().Add(optimisationRequest);

                foreach (var itineraire in itineraires)
                {
                    itineraire.OrganisationId = request.OrganisationId;
                    itineraire.Statut = StatutItineraire.EnAttente;
                    itineraire.DateCreation = DateTime.UtcNow;
                    _context.Set<Itineraire>().Add(itineraire);
                }

                // Marquer les véhicules comme utilisés
                foreach (var vehicule in vehicules)
                {
                    vehicule.DerniereUtilisation = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // 8. RÉPONSE ENRICHIE
                var response = new OptimisationResponseDto
                {
                    OptimisationRequestId = optimisationRequest.Id,
                    DepotUtilise = depotEntity.Nom,
                    DepotAdresse = depotEntity.Adresse,
                    ZoneGeographique = zoneGeographique,
                    NombreVehicules = vehicules.Count,
                    NombreItineraires = itineraires.Count,
                    NombrePointsCollectes = pointsDechets.Count,
                    DistanceTotale = itineraires.Sum(i => i.DistanceTotale),
                    DureeTotale = TimeSpan.FromSeconds(itineraires.Sum(i => i.DureeEstimee.TotalSeconds)),
                    CarburantTotal = itineraires.Sum(i => i.CarburantEstime),
                    Itineraires = itineraires.Select((it, index) => new ItineraireDto
                    {
                        Id = it.Id,
                        VehiculeNumero = index + 1,
                        VehiculeInfo = vehicules.ElementAtOrDefault(index)?.Immatriculation ?? "N/A",
                        VehiculeType = vehicules.ElementAtOrDefault(index)?.Type.ToString() ?? "N/A",
                        NombrePoints = it.ListePoints.Count,
                        DistanceKm = Math.Round(it.DistanceTotale, 2),
                        DureeEstimee = it.DureeEstimee.ToString(@"hh\:mm\:ss"),
                        CarburantLitres = Math.Round(it.CarburantEstime, 2),
                        Points = it.ListePoints.Select(p => new PointDechetSimpleDto
                        {
                            Id = p.Id,
                            Latitude = p.Latitude,
                            Longitude = p.Longitude,
                            Type = p.Type?.ToString(),
                            Volume = p.VolumeEstime ?? 0,
                            Zone = p.Zone
                        }).ToList()
                    }).ToList(),
                    ScoreEfficacite = CalculerScoreEfficacite(itineraires, pointsDechets.Count)
                };

                _logger.LogInformation("Optimisation terminée: {Count} itinéraires créés", response.NombreItineraires);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'optimisation");
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }

        /// <summary>
        /// Détermine la zone géographique via Nominatim (reverse geocoding)
        /// Utilise la même méthode que PointDechetController
        /// </summary>
        private async Task<string> DeterminerZoneGeographiqueAsync(double latitude, double longitude)
        {
            try
            {
                var (zone, _) = await GetReverseGeocodingAsync(latitude, longitude);
                return string.IsNullOrWhiteSpace(zone) || zone == "Inconnue" ? "Zone non identifiée" : zone;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erreur lors du reverse geocoding pour ({Lat}, {Lon})", latitude, longitude);
                return "Zone non identifiée";
            }
        }

        /// <summary>
        /// Reverse geocoding via Nominatim (OpenStreetMap)
        /// Identique à la méthode dans PointDechetController
        /// </summary>
        private async Task<(string Zone, string Pays)> GetReverseGeocodingAsync(double latitude, double longitude)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "MyEcologicApp/1.0");
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var latStr = latitude.ToString("F6", CultureInfo.InvariantCulture);
            var lonStr = longitude.ToString("F6", CultureInfo.InvariantCulture);

            var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latStr}&lon={lonStr}&zoom=18&addressdetails=1";

            _logger.LogInformation("Appel Nominatim: {Url}", url);

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Nominatim a retourné le code: {StatusCode}", response.StatusCode);
                throw new Exception($"Nominatim error: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Réponse Nominatim reçue");

            var data = System.Text.Json.JsonDocument.Parse(json);

            if (!data.RootElement.TryGetProperty("address", out var address))
            {
                _logger.LogWarning("Pas de propriété 'address' dans la réponse Nominatim");
                return ("Inconnue", "Inconnu");
            }

            // Extraction de la zone (priorité: suburb > neighbourhood > quarter > city > town > village)
            var zone = address.TryGetProperty("suburb", out var suburb) ? suburb.GetString() :
                       address.TryGetProperty("neighbourhood", out var neighbourhood) ? neighbourhood.GetString() :
                       address.TryGetProperty("quarter", out var quarter) ? quarter.GetString() :
                       address.TryGetProperty("city", out var city) ? city.GetString() :
                       address.TryGetProperty("town", out var town) ? town.GetString() :
                       address.TryGetProperty("village", out var village) ? village.GetString() :
                       address.TryGetProperty("municipality", out var municipality) ? municipality.GetString() : "Inconnue";

            var pays = address.TryGetProperty("country", out var country) ? country.GetString() : "Inconnu";

            _logger.LogInformation("Résultat geocoding: Zone={Zone}, Pays={Pays}", zone, pays);

            return (zone ?? "Inconnue", pays ?? "Inconnu");
        }

        /// <summary>
        /// Calcule la distance entre deux points GPS (formule de Haversine)
        /// </summary>
        private double CalculerDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Rayon de la Terre en km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;

        /// <summary>
        /// Calcule un score d'efficacité basé sur la distance moyenne par point
        /// </summary>
        private double CalculerScoreEfficacite(List<Itineraire> itineraires, int totalPoints)
        {
            if (!itineraires.Any()) return 0;
            var distanceMoyenneParPoint = itineraires.Sum(i => i.DistanceTotale) / totalPoints;
            var score = Math.Max(0, 100 - (distanceMoyenneParPoint * 20));
            return Math.Round(score, 2);
        }
    }
}