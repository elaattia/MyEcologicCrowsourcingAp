// Controllers/PublicStatsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Data;
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublicStatsController : ControllerBase
    {
        private readonly EcologicDbContext _context;
        private readonly ILogger<PublicStatsController> _logger;

        public PublicStatsController(EcologicDbContext context, ILogger<PublicStatsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Récupère les statistiques publiques pour la page d'accueil
        /// Accessible sans authentification
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PublicStatsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublicStats()
        {
            try
            {
                // Total des déchets signalés (tous statuts confondus)
                var totalDechetsSignales = await _context.PointDechets.CountAsync();

                // Total des zones nettoyées (déchets avec statut "Nettoyé")
                var totalZonesNettoyees = await _context.PointDechets
                    .Where(p => p.Statut == StatutDechet.Nettoye)
                    .CountAsync();

                // Total des contributeurs actifs (tous les utilisateurs)
                // Citoyens + Représentants (les admins ne comptent généralement pas comme contributeurs)
                var totalContributeurs = await _context.Users
                    .Where(u => u.Role == UserRole.User || u.Role == UserRole.Representant)
                    .CountAsync();

                var response = new PublicStatsResponse
                {
                    TotalDechetsSignales = totalDechetsSignales,
                    TotalZonesNettoyees = totalZonesNettoyees,
                    TotalContributeursActifs = totalContributeurs
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques publiques");
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }

        /// <summary>
        /// Récupère des statistiques détaillées pour la page d'accueil
        /// </summary>
        [HttpGet("detailed")]
        [ProducesResponseType(typeof(DetailedPublicStatsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDetailedPublicStats()
        {
            try
            {
                var totalDechetsSignales = await _context.PointDechets.CountAsync();
                var totalZonesNettoyees = await _context.PointDechets
                    .Where(p => p.Statut == StatutDechet.Nettoye)
                    .CountAsync();
                var totalContributeurs = await _context.Users
                    .Where(u => u.Role == UserRole.User || u.Role == UserRole.Representant)
                    .CountAsync();

                // Statistiques supplémentaires
                var totalOrganisations = await _context.Organisations.CountAsync();
                var totalCitoyens = await _context.Users
                    .Where(u => u.Role == UserRole.User)
                    .CountAsync();

                // Déchets signalés ce mois
                var debutMois = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var dechetsCeMois = await _context.PointDechets
                    .Where(p => p.Date >= debutMois)
                    .CountAsync();

                // Zones les plus actives (top 5)
                var topZones = await _context.PointDechets
                    .GroupBy(p => p.Zone)
                    .Select(g => new ZoneStats
                    {
                        Zone = g.Key,
                        TotalDechets = g.Count(),
                        DechetsNettoyés = g.Count(p => p.Statut == StatutDechet.Nettoye)
                    })
                    .OrderByDescending(z => z.TotalDechets)
                    .Take(5)
                    .ToListAsync();

                var response = new DetailedPublicStatsResponse
                {
                    TotalDechetsSignales = totalDechetsSignales,
                    TotalZonesNettoyees = totalZonesNettoyees,
                    TotalContributeursActifs = totalContributeurs,
                    TotalOrganisations = totalOrganisations,
                    TotalCitoyens = totalCitoyens,
                    DechetsCeMois = dechetsCeMois,
                    TopZones = topZones
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques détaillées");
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }
    }

    public class PublicStatsResponse
    {
        public int TotalDechetsSignales { get; set; }
        public int TotalZonesNettoyees { get; set; }
        public int TotalContributeursActifs { get; set; }
    }

    public class DetailedPublicStatsResponse
    {
        public int TotalDechetsSignales { get; set; }
        public int TotalZonesNettoyees { get; set; }
        public int TotalContributeursActifs { get; set; }
        public int TotalOrganisations { get; set; }
        public int TotalCitoyens { get; set; }
        public int DechetsCeMois { get; set; }
        public List<ZoneStats> TopZones { get; set; } = new List<ZoneStats>();
    }

    public class ZoneStats
    {
        public string Zone { get; set; } = string.Empty;
        public int TotalDechets { get; set; }
        public int DechetsNettoyés { get; set; }
    }
}