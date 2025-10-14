using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Data;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Services;
using MyEcologicCrowsourcingApp.DTOs;

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

        /// <summary>
        /// Optimiser les tournées de collecte pour une organisation
        /// </summary>
        [HttpPost("optimiser")]
        public async Task<IActionResult> OptimiserTournees([FromBody] OptimisationRequestDto request)
        {
            try
            {
                _logger.LogInformation("Début optimisation pour organisation {OrgId}", request.OrganisationId);

                // Récupérer l'organisation avec ses véhicules
                var organisation = await _context.Set<Organisation>()
                    .Include(o => o.Vehicule)
                    .FirstOrDefaultAsync(o => o.OrganisationId == request.OrganisationId);

                if (organisation == null)
                {
                    return NotFound(new { message = "Organisation non trouvée" });
                }

                // Récupérer les points de déchets signalés dans la zone
                var pointsDechets = await _context.PointDechets
                    .Where(p => p.Statut == StatutDechet.Signale)
                    .Where(p => string.IsNullOrEmpty(request.ZoneGeographique) || 
                               p.Zone.Contains(request.ZoneGeographique))
                    .ToListAsync();

                if (!pointsDechets.Any())
                {
                    return BadRequest(new { message = "Aucun point de déchet à collecter" });
                }

                _logger.LogInformation("Points trouvés: {Count}", pointsDechets.Count);

                // Créer la liste de véhicules (pour simplifier, on utilise le même véhicule plusieurs fois)
                var vehicules = new List<Vehicule>();
                for (int i = 0; i < request.NombreVehicules; i++)
                {
                    vehicules.Add(new Vehicule
                    {
                        Id = Guid.NewGuid(),
                        Type = organisation.Vehicule?.Type ?? TypeVehicule.Camion,
                        CapaciteMax = request.CapaciteVehicule > 0 ? request.CapaciteVehicule : 10.0,
                        VitesseMoyenne = 30.0,
                        CarburantConsommation = 8.0
                    });
                }

                // Définir le dépôt (par défaut, centre de la Tunisie ou premier point)
                var depot = new Location
                {
                    Latitude = request.DepotLatitude ?? 36.8065,
                    Longitude = request.DepotLongitude ?? 10.1815,
                    Name = "Dépôt Central"
                };

                // OPTIMISER avec VRP Service
                var itineraires = await _vrpService.OptimiserTournees(
                    pointsDechets,
                    vehicules,
                    depot);

                // Sauvegarder les résultats
                var optimisationRequest = new OptimisationRequest
                {
                    Id = Guid.NewGuid(),
                    OrganisationId = request.OrganisationId,
                    ListePointsIds = pointsDechets.Select(p => p.Id).ToList(),
                    CapaciteVehicule = request.CapaciteVehicule,
                    TempsMaxParTrajet = request.TempsMaxParTrajet,
                    ZoneGeographique = request.ZoneGeographique
                };

                _context.Set<OptimisationRequest>().Add(optimisationRequest);

                // Associer les itinéraires à l'organisation
                foreach (var itineraire in itineraires)
                {
                    itineraire.OrganisationId = request.OrganisationId;
                    _context.Set<Itineraire>().Add(itineraire);
                }

                await _context.SaveChangesAsync();

                // Préparer la réponse
                var response = new OptimisationResponseDto
                {
                    OptimisationRequestId = optimisationRequest.Id,
                    NombreItineraires = itineraires.Count,
                    DistanceTotale = itineraires.Sum(i => i.DistanceTotale),
                    DureeTotale = TimeSpan.FromSeconds(itineraires.Sum(i => i.DureeEstimee.TotalSeconds)),
                    CarburantTotal = itineraires.Sum(i => i.CarburantEstime),
                    Itineraires = itineraires.Select((it, index) => new ItineraireDto
                    {
                        Id = it.Id,
                        VehiculeNumero = index + 1,
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

                _logger.LogInformation("Optimisation terminée: {Count} itinéraires, {Distance} km total",
                    response.NombreItineraires, response.DistanceTotale);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'optimisation");
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }

        /// <summary>
        /// Récupérer tous les itinéraires d'une organisation
        /// </summary>
        [HttpGet("itineraires/{organisationId}")]
        public async Task<IActionResult> GetItineraires(Guid organisationId)
        {
            var itineraires = await _context.Set<Itineraire>()
                .Include(i => i.ListePoints)
                .Where(i => i.OrganisationId == organisationId)
                .OrderByDescending(i => i.Id)
                .Take(10)
                .ToListAsync();

            return Ok(itineraires);
        }

        /// <summary>
        /// Récupérer un itinéraire spécifique avec détails
        /// </summary>
        [HttpGet("itineraire/{id}")]
        public async Task<IActionResult> GetItineraire(Guid id)
        {
            var itineraire = await _context.Set<Itineraire>()
                .Include(i => i.ListePoints)
                .Include(i => i.Organisation)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (itineraire == null)
                return NotFound();

            return Ok(itineraire);
        }

        private double CalculerScoreEfficacite(List<Itineraire> itineraires, int totalPoints)
        {
            if (!itineraires.Any()) return 0;

            var distanceMoyenneParPoint = itineraires.Sum(i => i.DistanceTotale) / totalPoints;
            
            // Score sur 100: moins de distance par point = meilleur score
            // 100 points si < 2km par point, décroissant jusqu'à 0
            var score = Math.Max(0, 100 - (distanceMoyenneParPoint * 20));
            
            return Math.Round(score, 2);
        }
    }

}