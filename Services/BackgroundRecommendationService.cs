using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Data;
using MyEcologicCrowsourcingApp.Models;
using System.Collections.Concurrent;

namespace MyEcologicCrowsourcingApp.Services
{
    public class BackgroundRecommendationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundRecommendationService> _logger;
        private readonly ConcurrentQueue<Guid> _pointsDechetsQueue;
        private readonly SemaphoreSlim _signal;

        public BackgroundRecommendationService(
            IServiceProvider serviceProvider,
            ILogger<BackgroundRecommendationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _pointsDechetsQueue = new ConcurrentQueue<Guid>();
            _signal = new SemaphoreSlim(0);
        }

        public void EnqueueRecommandation(Guid pointDechetId)
        {
            _pointsDechetsQueue.Enqueue(pointDechetId);
            _signal.Release();
            _logger.LogInformation("📥 Point de déchet {Id} ajouté à la file de recommandations", pointDechetId);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Service de recommandations en arrière-plan démarré");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _signal.WaitAsync(stoppingToken);

                    if (_pointsDechetsQueue.TryDequeue(out var pointDechetId))
                    {
                        await GenererRecommandationAsync(pointDechetId);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Service de recommandations arrêté");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur dans le service de recommandations");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        private async Task GenererRecommandationAsync(Guid pointDechetId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EcologicDbContext>();
            var geminiAgent = scope.ServiceProvider.GetRequiredService<GeminiSemanticKernelAgent>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<BackgroundRecommendationService>>();

            try
            {
                logger.LogInformation("🤖 Génération de recommandation pour {Id}", pointDechetId);

                var pointDechet = await context.PointDechets.FindAsync(pointDechetId);
                if (pointDechet == null)
                {
                    logger.LogWarning("Point de déchet {Id} introuvable", pointDechetId);
                    return;
                }

                var contexte = await ConstruireContexteAsync(pointDechet, context);

                var recommandation = await geminiAgent.GenererRecommandationAsync(contexte);

                if (recommandation != null && recommandation.EstActive)
                {
                    context.RecommandationsEcologiques.Add(recommandation);
                    await context.SaveChangesAsync();

                    logger.LogInformation("✅ Recommandation générée avec succès pour {Id}: {Action}",
                        pointDechetId, recommandation.ActionRecommandee);
                }
                else
                {
                    logger.LogWarning("⚠️ Échec génération recommandation pour {Id}", pointDechetId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Erreur génération recommandation pour {Id}", pointDechetId);
            }
        }

        private async Task<ContexteRecommandation> ConstruireContexteAsync(
            PointDechet pointDechet, 
            EcologicDbContext context)
        {
            const double rayonKm = 5.0;
            const double degresParKm = 1.0 / 111.0;
            var rayonDegres = rayonKm * degresParKm;

            var dechetsProches = await context.PointDechets
                .Where(p => p.Id != pointDechet.Id &&
                           Math.Abs(p.Latitude - pointDechet.Latitude) < rayonDegres &&
                           Math.Abs(p.Longitude - pointDechet.Longitude) < rayonDegres)
                .CountAsync();

            var dateDebut = DateTime.UtcNow.AddMonths(-6);
            var historiqueNettoyages = await context.PointDechets
                .Where(p => p.Zone == pointDechet.Zone &&
                           p.Statut == StatutDechet.Nettoye &&
                           p.DateNettoyage.HasValue &&
                           p.DateNettoyage.Value >= dateDebut)
                .Select(p => p.DateNettoyage!.Value)
                .ToListAsync();

            var organisationActive = await context.Organisations
                .Include(o => o.Depots)
                .AnyAsync(o => o.Depots != null && o.Depots.Any(d =>
                    Math.Abs(d.Latitude - pointDechet.Latitude) < rayonDegres &&
                    Math.Abs(d.Longitude - pointDechet.Longitude) < rayonDegres));

            var saison = DeterminerSaison(DateTime.UtcNow);

            if (!pointDechet.VolumeEstime.HasValue)
            {
                pointDechet.VolumeEstime = EstimerVolume(pointDechet.Type);
            }

            return new ContexteRecommandation
            {
                PointDechet = pointDechet,
                NombreDechetsProches = dechetsProches,
                HistoriqueNettoyages = historiqueNettoyages,
                OrganisationLocaleActive = organisationActive,
                Saison = saison
            };
        }

        private string DeterminerSaison(DateTime date)
        {
            var mois = date.Month;
            return mois switch
            {
                12 or 1 or 2 => "Hiver",
                3 or 4 or 5 => "Printemps",
                6 or 7 or 8 => "Été",
                9 or 10 or 11 => "Automne",
                _ => "Inconnue"
            };
        }

        private double EstimerVolume(TypeDechet? type)
        {
            return type switch
            {
                TypeDechet.Plastique => 5.0,
                TypeDechet.Verre => 10.0,
                TypeDechet.Metale => 15.0,
                TypeDechet.Pile => 2.0,
                TypeDechet.Papier => 3.0,
                TypeDechet.Autre => 7.0,
                _ => 5.0
            };
        }
    }
}