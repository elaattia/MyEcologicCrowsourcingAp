using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyEcologicCrowsourcingApp.Services
{
    public class GeminiLangChainAgentFinal
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiSettings _geminiSettings;
        private readonly ILogger<GeminiLangChainAgentFinal> _logger;

        public GeminiLangChainAgentFinal(
            HttpClient httpClient,
            IOptions<GeminiSettings> geminiSettings,
            ILogger<GeminiLangChainAgentFinal> logger)
        {
            _httpClient = httpClient;
            _geminiSettings = geminiSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Génère une recommandation écologique basée sur le contexte du point de déchet
        /// </summary>
        public async Task<RecommandationEcologique> GenererRecommandationAsync(ContexteRecommandation contexte)
        {
            try
            {
                // Validation du contexte
                if (contexte?.PointDechet == null)
                {
                    _logger.LogWarning("Contexte ou PointDechet est null");
                    return CreerRecommandationParDefaut(Guid.Empty, "Contexte invalide");
                }

                _logger.LogInformation("Génération recommandation pour déchet {Id} - Zone: {Zone}, Type: {Type}", 
                    contexte.PointDechet.Id, 
                    contexte.PointDechet.Zone, 
                    contexte.PointDechet.Type);

                // Vérification de la clé API
                if (string.IsNullOrWhiteSpace(_geminiSettings.ApiKey))
                {
                    _logger.LogError("Clé API Gemini non configurée");
                    return CreerRecommandationParDefaut(contexte.PointDechet.Id, "Clé API manquante");
                }

                var prompt = ConstruirePrompt(contexte);
                _logger.LogDebug("Prompt construit: {Prompt}", prompt);

                // Liste des modèles à essayer dans l'ordre
                var modelsToTry = new[]
                {
                    "gemini-2.0-flash-exp",
                    "gemini-1.5-flash",
                    "gemini-1.5-flash-latest",
                    "gemini-1.5-pro",
                    "gemini-1.5-pro-latest",
                    "gemini-pro"
                };

                foreach (var model in modelsToTry)
                {
                    _logger.LogInformation("Tentative avec le modèle: {Model}", model);
                    
                    var result = await TenterAppelGeminiAsync(contexte, model, prompt);
                    if (result != null)
                    {
                        _logger.LogInformation("✅ Succès avec le modèle {Model}", model);
                        return result;
                    }
                }

                // Si tous les modèles échouent
                _logger.LogWarning("Tous les modèles Gemini ont échoué, utilisation de la recommandation par défaut");
                return CreerRecommandationParDefaut(contexte.PointDechet.Id, "Aucun modèle Gemini disponible");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la génération de recommandation");
                return CreerRecommandationParDefaut(
                    contexte?.PointDechet?.Id ?? Guid.Empty, 
                    $"Erreur: {ex.Message}");
            }
        }

        /// <summary>
        /// Tente un appel à l'API Gemini avec un modèle spécifique
        /// </summary>
        private async Task<RecommandationEcologique?> TenterAppelGeminiAsync(
            ContexteRecommandation contexte, 
            string model, 
            string prompt)
        {
            try
            {
                var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_geminiSettings.ApiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.8,  // Plus créatif pour des impacts variés
                        maxOutputTokens = 150,  // Réduit pour forcer la concision
                        topP = 0.95,
                        topK = 40
                    },
                    safetySettings = new[]
                    {
                        new
                        {
                            category = "HARM_CATEGORY_HARASSMENT",
                            threshold = "BLOCK_NONE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_HATE_SPEECH",
                            threshold = "BLOCK_NONE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                            threshold = "BLOCK_NONE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                            threshold = "BLOCK_NONE"
                        }
                    }
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.Timeout = TimeSpan.FromSeconds(30);

                _logger.LogInformation("📡 Appel à l'API Gemini avec {Model}...", model);

                var response = await _httpClient.PostAsJsonAsync(apiUrl, requestBody);

                // Lire la réponse brute pour debugging
                var jsonResponse = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("📥 Status Code: {StatusCode}", response.StatusCode);
                _logger.LogInformation("📥 Réponse brute Gemini ({Length} chars): {Response}", 
                    jsonResponse.Length, 
                    jsonResponse.Length > 500 ? jsonResponse.Substring(0, 500) + "..." : jsonResponse);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("❌ Échec avec {Model}: {StatusCode}", model, response.StatusCode);
                    return null;
                }

                // Parser la réponse JSON
                var json = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                // Afficher la structure complète pour debugging
                _logger.LogDebug("Structure JSON complète: {Json}", json.ToString());

                // Vérifier si la réponse contient une erreur
                if (json.TryGetProperty("error", out var error))
                {
                    var errorMessage = error.GetProperty("message").GetString();
                    _logger.LogWarning("❌ Erreur Gemini: {Error}", errorMessage);
                    return null;
                }

                // Essayer plusieurs chemins de parsing
                string? texteReponse = null;

                // Méthode 1: candidates[0].content.parts[0].text
                if (json.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    _logger.LogDebug("✓ Propriété 'candidates' trouvée avec {Count} éléments", candidates.GetArrayLength());
                    
                    var firstCandidate = candidates[0];
                    _logger.LogDebug("Premier candidat: {Candidate}", firstCandidate.ToString());

                    if (firstCandidate.TryGetProperty("content", out var content))
                    {
                        _logger.LogDebug("✓ Propriété 'content' trouvée");
                        
                        if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                        {
                            _logger.LogDebug("✓ Propriété 'parts' trouvée avec {Count} éléments", parts.GetArrayLength());
                            
                            var firstPart = parts[0];
                            if (firstPart.TryGetProperty("text", out var textElement))
                            {
                                texteReponse = textElement.GetString();
                                _logger.LogInformation("✅ Texte extrait avec succès: {Length} caractères", texteReponse?.Length ?? 0);
                            }
                            else
                            {
                                _logger.LogWarning("❌ Propriété 'text' non trouvée dans parts[0]");
                                _logger.LogDebug("Structure de parts[0]: {Part}", firstPart.ToString());
                            }
                        }
                        else
                        {
                            _logger.LogWarning("❌ Propriété 'parts' non trouvée ou vide");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("❌ Propriété 'content' non trouvée");
                    }

                    // Vérifier si le contenu a été bloqué
                    if (firstCandidate.TryGetProperty("finishReason", out var finishReason))
                    {
                        var reason = finishReason.GetString();
                        _logger.LogInformation("Finish reason: {Reason}", reason);
                        
                        if (reason == "SAFETY" || reason == "RECITATION")
                        {
                            _logger.LogWarning("⚠️ Contenu bloqué par les filtres de sécurité: {Reason}", reason);
                            return null;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("❌ Propriété 'candidates' non trouvée ou vide");
                    _logger.LogDebug("Propriétés disponibles: {Props}", string.Join(", ", 
                        json.EnumerateObject().Select(p => p.Name)));
                }

                if (string.IsNullOrWhiteSpace(texteReponse))
                {
                    _logger.LogWarning("❌ Impossible d'extraire le texte de la réponse");
                    return null;
                }

                _logger.LogInformation("✅ Recommandation générée avec succès: {Length} caractères", texteReponse.Length);
                _logger.LogDebug("Contenu: {Text}", texteReponse);

                // Créer la recommandation avec les données de Gemini
                var recommandation = new RecommandationEcologique
                {
                    Id = Guid.NewGuid(),
                    PointDechetId = contexte.PointDechet.Id,
                    ScorePriorite = CalculerScorePriorite(contexte),
                    Urgence = CalculerUrgence(contexte),
                    ActionRecommandee = Nettoyer(texteReponse),
                    Justification = $"Analyse IA Gemini ({model}) - {contexte.NombreDechetsProches} déchets proches, " +
                                  $"Organisation: {(contexte.OrganisationLocaleActive ? "Oui" : "Non")}, " +
                                  $"Saison: {contexte.Saison}",
                    ContexteUtilise = SerializerContexte(contexte),
                    DateGeneration = DateTime.UtcNow,
                    EstActive = true
                };

                return recommandation;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogWarning(httpEx, "Erreur réseau avec le modèle {Model}", model);
                return null;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "Erreur de parsing JSON avec le modèle {Model}", model);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erreur avec le modèle {Model}", model);
                return null;
            }
        }

        /// <summary>
        /// Crée une recommandation par défaut en cas d'erreur
        /// </summary>
        private RecommandationEcologique CreerRecommandationParDefaut(Guid pointDechetId, string raison)
        {
            _logger.LogWarning("⚠️ Création d'une recommandation par défaut pour {Id}: {Raison}", pointDechetId, raison);

            return new RecommandationEcologique
            {
                Id = Guid.NewGuid(),
                PointDechetId = pointDechetId,
                ScorePriorite = 50,
                Urgence = "Moyenne",
                ActionRecommandee = "Inspection et évaluation requises. Contacter les services locaux de gestion des déchets pour une évaluation détaillée.",
                Justification = $"Recommandation automatique générée ({raison}). Une analyse manuelle est recommandée.",
                DateGeneration = DateTime.UtcNow,
                EstActive = true
            };
        }

        /// <summary>
        /// Construit le prompt pour Gemini
        /// </summary>
        private string ConstruirePrompt(ContexteRecommandation contexte)
        {
            var typeDechet = contexte.PointDechet.Type?.ToString() ?? "Non classifié";
            var volume = contexte.PointDechet.VolumeEstime ?? 0;
            var pays = contexte.PointDechet.Pays ?? "Inconnu";

            // Adapter le message selon le type de déchet
            string focusMessage = typeDechet switch
            {
                "Non classifié" => "Déchet non identifié détecté. Évalue les RISQUES POTENTIELS (pollution, santé publique) et les MESURES D'URGENCE nécessaires.",
                "Pile" => "Piles détectées - DANGER TOXIQUE. Décris les RISQUES pour l'environnement et la santé, et les ACTIONS URGENTES requises.",
                _ => $"Déchet de type {typeDechet} détecté. Analyse les IMPACTS ENVIRONNEMENTAUX et SANITAIRES, puis propose des ACTIONS CONCRÈTES."
            };

            return $@"Tu es un expert environnemental. {focusMessage}

CONTEXTE:
- Type: {typeDechet}
- Localisation: {contexte.PointDechet.Zone}, {pays}
- Volume: {volume:F1} kg
- Déchets similaires proches: {contexte.NombreDechetsProches}
- Organisation locale: {(contexte.OrganisationLocaleActive ? "présente" : "absente")}

RÉPONSE (max 400 caractères, en UN paragraphe):
1. Risques environnementaux et sanitaires spécifiques à ce type de déchet
2. Actions urgentes à entreprendre
3. Conséquences si non traité

Sois alarmiste si nécessaire, concret et direct. Focus sur l'IMPACT, pas la logistique.";
        }

        /// <summary>
        /// Calcule le score de priorité basé sur le contexte
        /// </summary>
        private int CalculerScorePriorite(ContexteRecommandation contexte)
        {
            int score = 50; // Score de base

            // Volume du déchet
            if (contexte.PointDechet.VolumeEstime.HasValue)
            {
                var volume = contexte.PointDechet.VolumeEstime.Value;
                if (volume > 100) score += 30;
                else if (volume > 50) score += 20;
                else if (volume > 20) score += 10;
            }

            // Concentration de déchets
            if (contexte.NombreDechetsProches > 20) score += 20;
            else if (contexte.NombreDechetsProches > 10) score += 10;
            else if (contexte.NombreDechetsProches > 5) score += 5;

            // Historique de nettoyage (zone négligée)
            if (contexte.HistoriqueNettoyages.Count == 0) score += 15;
            else if (contexte.HistoriqueNettoyages.Count < 3) score += 5;

            // Organisation locale (facilite le nettoyage)
            if (contexte.OrganisationLocaleActive) score += 10;

            // Type de déchet dangereux
            if (contexte.PointDechet.Type == TypeDechet.Pile) score += 25;
            else if (contexte.PointDechet.Type == TypeDechet.Metale) score += 15;

            return Math.Min(score, 100); // Limiter à 100
        }

        /// <summary>
        /// Calcule le niveau d'urgence
        /// </summary>
        private string CalculerUrgence(ContexteRecommandation contexte)
        {
            var score = CalculerScorePriorite(contexte);

            if (score >= 80) return "Critique";
            if (score >= 60) return "Haute";
            if (score >= 40) return "Moyenne";
            return "Basse";
        }

        /// <summary>
        /// Nettoie le texte de la réponse et limite sa longueur
        /// </summary>
        private string Nettoyer(string texte)
        {
            var cleaned = texte
                .Trim()
                .Replace("\n\n\n", "\n\n")
                .Replace("\n", " ")  // Tout mettre sur une ligne
                .Replace("**", "")
                .Replace("*", "")
                .Replace("  ", " ");  // Supprimer doubles espaces
            
            // Limiter à 480 caractères pour laisser de la marge (max DB = 500)
            if (cleaned.Length > 480)
            {
                _logger.LogWarning("Texte tronqué de {Original} à 480 caractères", cleaned.Length);
                
                // Trouver la dernière phrase complète
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

        /// <summary>
        /// Sérialise le contexte pour stockage
        /// </summary>
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

                return JsonSerializer.Serialize(contexteSimplifie, new JsonSerializerOptions 
                { 
                    WriteIndented = false 
                });
            }
            catch
            {
                return "{}";
            }
        }
    }
}