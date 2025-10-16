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

        [HttpPost("optimiser")]
        public async Task<IActionResult> OptimiserTournees([FromBody] OptimisationRequestDto request)
        {
            try
            {
                _logger.LogInformation("Début optimisation pour organisation {OrgId}", request.OrganisationId);

                // 1. RÉCUPÉRER L'ORGANISATION
                var organisation = await _context.Set<Organisation>()
                    .Include(o => o.Vehicules)
                    .Include(o => o.Depots)
                    .FirstOrDefaultAsync(o => o.OrganisationId == request.OrganisationId);

                if (organisation == null)
                    return NotFound(new { message = "Organisation non trouvée" });

                // 2. RÉCUPÉRER LES VÉHICULES DYNAMIQUEMENT
                List<Vehicule> vehicules;
                
                if (request.VehiculesIds?.Any() == true)
                {
                    // Option A : Véhicules spécifiques
                    vehicules = await _context.Set<Vehicule>()
                        .Where(v => request.VehiculesIds.Contains(v.Id))
                        .Where(v => v.OrganisationId == request.OrganisationId)
                        .Where(v => v.EstDisponible)
                        .ToListAsync();

                    if (!vehicules.Any())
                        return BadRequest(new { message = "Aucun véhicule disponible parmi ceux sélectionnés" });
                }
                else if (request.NombreVehicules.HasValue && request.NombreVehicules > 0)
                {
                    // Option B : Prendre N véhicules disponibles
                    vehicules = await _context.Set<Vehicule>()
                        .Where(v => v.OrganisationId == request.OrganisationId)
                        .Where(v => v.EstDisponible)
                        .Take(request.NombreVehicules.Value)
                        .ToListAsync();

                    if (vehicules.Count < request.NombreVehicules.Value)
                    {
                        return BadRequest(new { 
                            message = $"Seulement {vehicules.Count} véhicules disponibles sur {request.NombreVehicules.Value} demandés"
                        });
                    }
                }
                else
                {
                    // Option C : Tous les véhicules disponibles
                    vehicules = await _context.Set<Vehicule>()
                        .Where(v => v.OrganisationId == request.OrganisationId)
                        .Where(v => v.EstDisponible)
                        .ToListAsync();

                    if (!vehicules.Any())
                        return BadRequest(new { message = "Aucun véhicule disponible pour cette organisation" });
                }

                _logger.LogInformation("Véhicules sélectionnés: {Count}", vehicules.Count);

                // 3. RÉCUPÉRER LE DÉPÔT DYNAMIQUEMENT
                Location depot;
                
                if (request.DepotId.HasValue)
                {
                    // Option A : Dépôt existant
                    var depotEntity = await _context.Set<Depot>()
                        .FirstOrDefaultAsync(d => d.Id == request.DepotId.Value 
                                                && d.OrganisationId == request.OrganisationId
                                                && d.EstActif);

                    if (depotEntity == null)
                        return BadRequest(new { message = "Dépôt introuvable ou inactif" });

                    depot = new Location
                    {
                        Latitude = depotEntity.Latitude,
                        Longitude = depotEntity.Longitude,
                        Name = depotEntity.Nom
                    };
                }
                else if (request.DepotLatitude.HasValue && request.DepotLongitude.HasValue)
                {
                    // Option B : Coordonnées manuelles
                    depot = new Location
                    {
                        Latitude = request.DepotLatitude.Value,
                        Longitude = request.DepotLongitude.Value,
                        Name = "Dépôt Temporaire"
                    };
                }
                else
                {
                    // Option C : Premier dépôt de l'organisation
                    var depotParDefaut = await _context.Set<Depot>()
                        .FirstOrDefaultAsync(d => d.OrganisationId == request.OrganisationId 
                                                && d.EstActif);

                    if (depotParDefaut == null)
                    {
                        // Fallback : Centre de la Tunisie
                        depot = new Location
                        {
                            Latitude = 36.8065,
                            Longitude = 10.1815,
                            Name = "Dépôt Par Défaut"
                        };
                        _logger.LogWarning("Aucun dépôt configuré, utilisation coordonnées par défaut");
                    }
                    else
                    {
                        depot = new Location
                        {
                            Latitude = depotParDefaut.Latitude,
                            Longitude = depotParDefaut.Longitude,
                            Name = depotParDefaut.Nom
                        };
                    }
                }

                _logger.LogInformation("Dépôt: {Name} ({Lat}, {Lon})", depot.Name, depot.Latitude, depot.Longitude);

                // 4. RÉCUPÉRER LES POINTS DE DÉCHETS
                var query = _context.PointDechets
                    .Where(p => p.Statut == StatutDechet.Signale);

                if (!string.IsNullOrEmpty(request.ZoneGeographique))
                {
                    var zone = request.ZoneGeographique.ToLower();
                    query = query.Where(p => 
                        p.Zone.ToLower().Contains(zone) || 
                        p.Zone == "Inconnue");
                }

                var pointsDechets = await query.ToListAsync();

                if (!pointsDechets.Any())
                {
                    return BadRequest(new { 
                        message = "Aucun point de déchet à collecter",
                        info = "Tous les déchets ont été nettoyés ou aucun déchet dans cette zone"
                    });
                }

                _logger.LogInformation("Points disponibles: {Count}", pointsDechets.Count);

                // 5. OPTIMISER
                var itineraires = await _vrpService.OptimiserTournees(
                    pointsDechets,
                    vehicules,
                    depot);

                // 6. SAUVEGARDER
                var optimisationRequest = new OptimisationRequest
                {
                    Id = Guid.NewGuid(),
                    OrganisationId = request.OrganisationId,
                    ListePointsIds = pointsDechets.Select(p => p.Id).ToList(),
                    CapaciteVehicule = vehicules.Average(v => v.CapaciteMax),
                    TempsMaxParTrajet = request.TempsMaxParTrajet,
                    ZoneGeographique = request.ZoneGeographique
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

                // 7. RÉPONSE
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

                _logger.LogInformation("Optimisation terminée: {Count} itinéraires", response.NombreItineraires);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'optimisation");
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }

        private double CalculerScoreEfficacite(List<Itineraire> itineraires, int totalPoints)
        {
            if (!itineraires.Any()) return 0;
            var distanceMoyenneParPoint = itineraires.Sum(i => i.DistanceTotale) / totalPoints;
            var score = Math.Max(0, 100 - (distanceMoyenneParPoint * 20));
            return Math.Round(score, 2);
        }
    }
}