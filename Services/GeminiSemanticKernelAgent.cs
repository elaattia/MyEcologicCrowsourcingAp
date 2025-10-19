using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Data;
using System.ComponentModel;
using System.Text.Json;

namespace MyEcologicCrowsourcingApp.Services
{
    public class GeminiSemanticKernelAgent
    {
        private readonly ILogger<GeminiSemanticKernelAgent> _logger;
        private readonly GeminiSettings _geminiSettings;
        private readonly EcologicDbContext _context;
        private readonly Kernel _kernel;

        public GeminiSemanticKernelAgent(
            ILogger<GeminiSemanticKernelAgent> logger,
            IOptions<GeminiSettings> geminiSettings,
            EcologicDbContext context,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _geminiSettings = geminiSettings.Value;
            _context = context;

            var builder = Kernel.CreateBuilder();
            
            var httpClient = httpClientFactory.CreateClient("GeminiClient");
            httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/");
            
            builder.AddOpenAIChatCompletion(
                modelId: "gemini-2.0-flash-exp",
                apiKey: _geminiSettings.ApiKey,
                httpClient: httpClient
            );

            builder.Plugins.AddFromType<WasteAnalysisPlugin>();
            
            _kernel = builder.Build();
        }

        public async Task<RecommandationEcologique> GenererRecommandationAsync(
            ContexteRecommandation contexte)
        {
            try
            {
                _logger.LogInformation("🤖 Démarrage Semantic Kernel Agent pour déchet {Id}", 
                    contexte.PointDechet.Id);

                var chatService = _kernel.GetRequiredService<IChatCompletionService>();
                var chatHistory = new ChatHistory();

                chatHistory.AddSystemMessage(@"Tu es un agent expert en gestion environnementale des déchets.

OBJECTIF: Analyser un point de déchet signalé et générer une recommandation d'action.

OUTILS DISPONIBLES:
- analyze_waste_impact: Analyse l'impact environnemental
- calculate_priority_score: Calcule le score de priorité
- check_nearby_waste: Évalue la concentration de déchets
- get_local_context: Récupère le contexte local

CONTRAINTES:
- Recommandation CONCISE (max 400 caractères)
- Focalisée sur l'IMPACT et l'ACTION
- Score de priorité 0-100

PROCESSUS:
1. Utilise les outils pour analyser
2. Synthétise une recommandation claire
3. Justifie le score de priorité");

                var question = $@"Analyse ce point de déchet et génère une recommandation:

Type: {contexte.PointDechet.Type}
Volume: {contexte.PointDechet.VolumeEstime}kg
Zone: {contexte.PointDechet.Zone}, {contexte.PointDechet.Pays}
Déchets proches: {contexte.NombreDechetsProches}
Organisation locale: {(contexte.OrganisationLocaleActive ? "Active" : "Inactive")}
Saison: {contexte.Saison}
Nettoyages récents: {contexte.HistoriqueNettoyages.Count}

Fournis une recommandation d'action prioritaire.";

                chatHistory.AddUserMessage(question);

                var settings = new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.8,
                    MaxTokens = 500,
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                };

                var response = await chatService.GetChatMessageContentAsync(
                    chatHistory,
                    settings,
                    _kernel
                );

                _logger.LogInformation("✅ Agent terminé. Réponse: {Response}", response.Content);

                var scorePriorite = ExtraireOuCalculerScore(response, contexte);

                var recommandation = new RecommandationEcologique
                {
                    Id = Guid.NewGuid(),
                    PointDechetId = contexte.PointDechet.Id,
                    ScorePriorite = scorePriorite,
                    Urgence = DeterminerUrgence(scorePriorite, contexte),
                    ActionRecommandee = NettoyerReponse(response.Content ?? "Évaluation requise"),
                    Justification = $"Analyse Semantic Kernel - {contexte.NombreDechetsProches} déchets proches, Org: {(contexte.OrganisationLocaleActive ? "Active" : "Inactive")}",
                    ContexteUtilise = SerializerContexte(contexte),
                    DateGeneration = DateTime.UtcNow,
                    EstActive = true
                };

                _logger.LogInformation("✅ Recommandation générée avec Semantic Kernel");

                return recommandation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur Semantic Kernel Agent");
                return CreerRecommandationParDefaut(contexte.PointDechet.Id, ex.Message);
            }
        }

        private int ExtraireOuCalculerScore(ChatMessageContent response, ContexteRecommandation contexte)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                response.Content ?? "", 
                @"score[:\s]+(\d+)"
            );

            if (match.Success && int.TryParse(match.Groups[1].Value, out var score))
            {
                return Math.Clamp(score, 0, 100);
            }

            return CalculerScorePriorite(contexte);
        }

        private int CalculerScorePriorite(ContexteRecommandation contexte)
        {
            var score = 50; 

            score += contexte.PointDechet.Type switch
            {
                TypeDechet.Pile => 25,        // Piles = dangereux (métaux lourds)
                TypeDechet.Plastique => 20,   // Plastique = pollution persistante
                TypeDechet.Verre => 15,       // Verre = danger coupure
                TypeDechet.Metale => 12,      // Métal = recyclable mais polluant
                TypeDechet.Papier => 8,       // Papier = moins problématique
                TypeDechet.Autre => 10,       // Autre = indéterminé
                _ => 5
            };

            score += contexte.PointDechet.VolumeEstime switch
            {
                > 100 => 20,
                > 50 => 15,
                > 20 => 10,
                _ => 5
            };

            score += contexte.NombreDechetsProches switch
            {
                > 20 => 20,
                > 10 => 15,
                > 5 => 10,
                _ => 5
            };

            if (contexte.OrganisationLocaleActive)
                score -= 10;

            return Math.Clamp(score, 0, 100);
        }

        private string DeterminerUrgence(int score, ContexteRecommandation contexte)
        {
            return score switch
            {
                >= 80 => "Critique",
                >= 60 => "Élevée",
                >= 40 => "Moyenne",
                _ => "Faible"
            };
        }

        private string NettoyerReponse(string texte)
        {
            var cleaned = texte
                .Trim()
                .Replace("\n", " ")
                .Replace("  ", " ")
                .Replace("**", "")
                .Replace("*", "");

            if (cleaned.Length > 480)
            {
                var lastPeriod = cleaned.LastIndexOf('.', 480);
                if (lastPeriod > 300)
                {
                    cleaned = cleaned.Substring(0, lastPeriod + 1);
                }
                else
                {
                    cleaned = cleaned.Substring(0, 477) + "...";
                }
            }

            return cleaned;
        }

        private string SerializerContexte(ContexteRecommandation contexte)
        {
            try
            {
                var contexteSimplifie = new
                {
                    Zone = contexte.PointDechet.Zone,
                    Type = contexte.PointDechet.Type?.ToString(),
                    Volume = contexte.PointDechet.VolumeEstime,
                    DechetsProches = contexte.NombreDechetsProches,
                    NettoyagesRecents = contexte.HistoriqueNettoyages.Count,
                    OrganisationActive = contexte.OrganisationLocaleActive,
                    Saison = contexte.Saison
                };

                return JsonSerializer.Serialize(contexteSimplifie);
            }
            catch
            {
                return "{}";
            }
        }

        private RecommandationEcologique CreerRecommandationParDefaut(Guid pointDechetId, string raison)
        {
            _logger.LogWarning("⚠️ Recommandation par défaut pour {Id}: {Raison}", pointDechetId, raison);

            return new RecommandationEcologique
            {
                Id = Guid.NewGuid(),
                PointDechetId = pointDechetId,
                ScorePriorite = 50,
                Urgence = "Moyenne",
                ActionRecommandee = "Inspection et évaluation requises. Contacter les services locaux.",
                Justification = $"Recommandation automatique (Agent indisponible: {raison})",
                DateGeneration = DateTime.UtcNow,
                EstActive = true
            };
        }
    }

    public class WasteAnalysisPlugin
    {
        [KernelFunction("analyze_waste_impact")]
        [Description("Analyse l'impact environnemental et sanitaire d'un type de déchet")]
        public string AnalyzeWasteImpact(
            [Description("Type de déchet à analyser")] string wasteType)
        {
            return wasteType switch
            {
                "Pile" => "IMPACT CRITIQUE: Contient métaux lourds (mercure, cadmium, plomb). Contamination grave des sols et nappes. Nécessite collecte spécialisée URGENTE.",
                "Plastique" => "IMPACT ÉLEVÉ: Pollution persistante (50-500 ans). Microplastiques toxiques. Danger pour faune marine. Accumulation dans chaîne alimentaire.",
                "Verre" => "IMPACT MODÉRÉ: Non biodégradable mais inerte. Danger de coupure pour humains et animaux. Recyclable à 100%.",
                "Metale" or "Métallique" => "IMPACT MODÉRÉ: Contamination lente des sols. Recyclable. Risque de corrosion et libération de particules.",
                "Papier" => "IMPACT FAIBLE: Biodégradable mais émissions méthane en décomposition. Recyclable. Pollution visuelle.",
                "Autre" => "IMPACT À ÉVALUER: Type non identifié précisément. Inspection nécessaire pour déterminer dangerosité exacte.",
                _ => "IMPACT INDÉTERMINÉ: Classification requise pour évaluation correcte."
            };
        }

        [KernelFunction("calculate_priority_score")]
        [Description("Calcule le score de priorité d'intervention (0-100)")]
        public string CalculatePriorityScore(
            [Description("Volume en kg")] double volume,
            [Description("Nombre de déchets proches")] int nearbyWaste,
            [Description("Type de déchet")] string wasteType)
        {
            var score = 50;

            score += wasteType switch
            {
                "Pile" => 25,
                "Plastique" => 20,
                "Verre" => 15,
                "Metale" or "Métallique" => 12,
                "Papier" => 8,
                _ => 10
            };

            score += volume switch
            {
                > 100 => 20,
                > 50 => 15,
                _ => 5
            };

            score += nearbyWaste switch
            {
                > 20 => 20,
                > 10 => 15,
                _ => 5
            };

            return $"Score calculé: {Math.Clamp(score, 0, 100)}/100. Facteurs: Volume={volume}kg, Concentration={nearbyWaste} déchets, Type={wasteType}";
        }

        [KernelFunction("check_nearby_waste")]
        [Description("Évalue la concentration de déchets dans un rayon de 5km")]
        public string CheckNearbyWaste(
            [Description("Nombre de déchets détectés")] int count)
        {
            var niveau = count switch
            {
                > 20 => "CRITIQUE - Zone fortement polluée",
                > 10 => "ÉLEVÉ - Accumulation préoccupante",
                > 5 => "MODÉRÉ - Surveillance requise",
                _ => "FAIBLE - Déchet isolé"
            };

            return $"{count} déchets détectés dans un rayon de 5km. Niveau: {niveau}";
        }

        [KernelFunction("get_local_context")]
        [Description("Récupère le contexte local (réglementations, organisations)")]
        public string GetLocalContext(
            [Description("Zone géographique")] string zone,
            [Description("Pays")] string country,
            [Description("Présence d'organisation locale")] bool hasOrganization,
            [Description("Saison actuelle")] string season)
        {
            var orgStatus = hasOrganization 
                ? "Organisation locale ACTIVE - Coordination possible" 
                : "Aucune organisation - Action citoyenne ou autorités nécessaires";

            return $"Localisation: {zone}, {country}. {orgStatus}. Saison: {season} (impact sur accessibilité et urgence).";
        }
    }
}