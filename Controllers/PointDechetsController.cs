using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEcologicCrowsourcingApp.Models;
using System.Security.Claims;
using MyEcologicCrowsourcingApp.DTOs;
using MyEcologicCrowsourcingApp.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Text;
using MyEcologicCrowsourcingApp.Services;

namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PointDechetController : Controller
    {
        private readonly EcologicDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PointDechetController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiSemanticKernelAgent _geminiAgent;
        private readonly BackgroundRecommendationService _backgroundRecommendationService;

        public PointDechetController(
            EcologicDbContext context,
            IWebHostEnvironment env,
            ILogger<PointDechetController> logger,
            IHttpClientFactory httpClientFactory,
            GeminiSemanticKernelAgent geminiAgent,
    BackgroundRecommendationService backgroundRecommendationService)
        {
            _context = context;
            _env = env;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _geminiAgent = geminiAgent;
            _backgroundRecommendationService = backgroundRecommendationService;
        }

        [HttpPost("signaler")]
        public async Task<IActionResult> SignalerDechet([FromForm] SignalerDechetRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié" });
                }

                if (request.Image == null || request.Image.Length == 0)
                {
                    return BadRequest(new { message = "Une image est requise" });
                }

                double latitude, longitude;
                try
                {
                    latitude = request.GetLatitude();
                    longitude = request.GetLongitude();
                }
                catch (FormatException)
                {
                    return BadRequest(new { message = "Format de coordonnées GPS invalide" });
                }

                if (request.Image.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { message = "L'image ne doit pas dépasser 5MB" });
                }

                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
                if (!allowedTypes.Contains(request.Image.ContentType.ToLower()))
                {
                    return BadRequest(new { message = "Format non supporté (JPG, PNG, WEBP uniquement)" });
                }

                if (latitude < -90 || latitude > 90)
                {
                    return BadRequest(new { message = "Latitude invalide (doit être entre -90 et 90)" });
                }

                if (longitude < -180 || longitude > 180)
                {
                    return BadRequest(new { message = "Longitude invalide (doit être entre -180 et 180)" });
                }

                if (request.Image.Length < 10 * 1024)
                {
                    _logger.LogWarning("Image trop petite: {Size}KB", request.Image.Length / 1024);
                    return BadRequest(new { 
                        message = "L'image est trop petite (moins de 10KB). Veuillez utiliser une image de meilleure qualité pour une classification optimale." 
                    });
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "dechets");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.Image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.Image.CopyToAsync(fileStream);
                }

                var imageUrl = $"/uploads/dechets/{uniqueFileName}";

                string zone = "Inconnue";
                string pays = "Inconnu";

                try
                {
                    var geoResult = await GetReverseGeocodingAsync(latitude, longitude);
                    zone = geoResult.Zone;
                    pays = geoResult.Pays;

                    _logger.LogInformation($"Géolocalisation réussie: {zone}, {pays} pour coordonnées ({latitude}, {longitude})");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Échec reverse geocoding pour ({latitude}, {longitude}): {ex.Message}");
                }

                var pointDechet = new PointDechet
                {
                    Id = Guid.NewGuid(),
                    Url = imageUrl,
                    Latitude = latitude,
                    Longitude = longitude,
                    Statut = StatutDechet.Signale,
                    Type = null,
                    Date = DateTime.UtcNow,
                    UserId = userId,
                    Zone = zone,
                    Pays = pays,
                    VolumeEstime = null
                };

                _context.PointDechets.Add(pointDechet);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Déchet signalé avec succès: {Id}", pointDechet.Id);

                // === ÉTAPE 1: Classification automatique ===
                WasteClassificationResponse? classificationResult = null;
                try
                {
                    _logger.LogInformation("Démarrage de la classification automatique pour le déchet {Id}", pointDechet.Id);
                    classificationResult = await ClassifyWasteAsync(pointDechet.Id);

                    if (classificationResult != null && classificationResult.Success)
                    {
                        _logger.LogInformation("Classification automatique réussie: {Category} avec {Confidence}% de confiance",
                            classificationResult.Category, classificationResult.Confidence * 100);
                    }
                    else
                    {
                        _logger.LogWarning("La classification automatique a échoué ou n'a pas retourné de résultat");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la classification automatique du déchet {Id}. Le déchet reste signalé sans type.", pointDechet.Id);
                }

                // Recharger le point de déchet après classification
                var updatedPointDechet = await _context.PointDechets.FindAsync(pointDechet.Id);

                // === ÉTAPE 2: Génération de recommandations écologiques avec Gemini ===
                /*RecommandationEcologique? recommandation = null;
                try
                {
                    _logger.LogInformation("Démarrage de la génération de recommandations avec Gemini pour le déchet {Id}", pointDechet.Id);
                    
                    // Construire le contexte pour Gemini
                    var contexte = await ConstruireContexteRecommandationAsync(updatedPointDechet!);
                    
                    // Appeler l'agent Gemini
                    recommandation = await _geminiAgent.GenererRecommandationAsync(contexte);
                    
                    if (recommandation != null && recommandation.EstActive)
                    {
                        // Sauvegarder la recommandation dans la base de données
                        _context.RecommandationsEcologiques.Add(recommandation);
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Recommandation écologique générée avec succès pour le déchet {Id}: {Action}", 
                            pointDechet.Id, recommandation.ActionRecommandee);
                    }
                    else
                    {
                        _logger.LogWarning("La génération de recommandation a échoué pour le déchet {Id}", pointDechet.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la génération de recommandations pour le déchet {Id}", pointDechet.Id);
                }*/

                try
                {
                    _logger.LogInformation("📬 Envoi du déchet {Id} pour génération de recommandation en arrière-plan", updatedPointDechet!.Id);
                    
                    // Envoyer à la file d'attente pour traitement asynchrone
                    _backgroundRecommendationService.EnqueueRecommandation(updatedPointDechet.Id);
                    
                    _logger.LogInformation("✅ Déchet {Id} ajouté à la file de recommandations. Traitement en cours...", updatedPointDechet.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur lors de l'ajout en file d'attente pour {Id}", updatedPointDechet!.Id);
                }
                RecommandationEcologique? recommandation = null;

                return CreatedAtAction(nameof(GetPointDechet), new { id = pointDechet.Id }, new
                {
                    id = updatedPointDechet!.Id,
                    url = imageUrl,
                    latitude = updatedPointDechet.Latitude,
                    longitude = updatedPointDechet.Longitude,
                    zone = updatedPointDechet.Zone,
                    pays = updatedPointDechet.Pays,
                    statut = updatedPointDechet.Statut.ToString(),
                    type = updatedPointDechet.Type?.ToString(),
                    date = updatedPointDechet.Date,
                    classification = classificationResult != null ? new
                    {
                        category = classificationResult.Category,
                        confidence = classificationResult.Confidence,
                        roboflowClass = classificationResult.RoboflowClass,
                        detectedObjects = classificationResult.DetectedObjects,
                        fromCache = classificationResult.FromCache
                    } : null,
                    recommandation = recommandation != null ? new
                    {
                        id = recommandation.Id,
                        scorePriorite = recommandation.ScorePriorite,
                        urgence = recommandation.Urgence,
                        actionRecommandee = recommandation.ActionRecommandee,
                        justification = recommandation.Justification,
                        dateGeneration = recommandation.DateGeneration
                    } : null,
                    recommandationEnCours = true,
                    message = GenerateSuccessMessage(updatedPointDechet, classificationResult, recommandation)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du signalement: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }

        [HttpGet("{id}/check-recommandation")]
        public async Task<IActionResult> CheckRecommandation(Guid id)
        {
            var recommandation = await _context.RecommandationsEcologiques
                .Where(r => r.PointDechetId == id && r.EstActive)
                .OrderByDescending(r => r.DateGeneration)
                .FirstOrDefaultAsync();

            if (recommandation == null)
            {
                return Ok(new { 
                    status = "en_cours", 
                    message = "Recommandation en cours de génération",
                    pointDechetId = id
                });
            }

            return Ok(new { 
                status = "pret", 
                recommandation = new
                {
                    id = recommandation.Id,
                    scorePriorite = recommandation.ScorePriorite,
                    urgence = recommandation.Urgence,
                    actionRecommandee = recommandation.ActionRecommandee,
                    justification = recommandation.Justification,
                    dateGeneration = recommandation.DateGeneration
                }
            });
        }
        private async Task<ContexteRecommandation> ConstruireContexteRecommandationAsync(PointDechet pointDechet)
        {
            // Calculer le nombre de déchets proches (rayon de 5km environ)
            const double rayonKm = 5.0;
            const double degresParKm = 1.0 / 111.0; // Approximation simple
            var rayonDegres = rayonKm * degresParKm;

            var dechetsProches = await _context.PointDechets
                .Where(p => p.Id != pointDechet.Id &&
                           Math.Abs(p.Latitude - pointDechet.Latitude) < rayonDegres &&
                           Math.Abs(p.Longitude - pointDechet.Longitude) < rayonDegres)
                .CountAsync();

            // Récupérer l'historique des nettoyages dans la zone (6 derniers mois)
            var dateDebut = DateTime.UtcNow.AddMonths(-6);
            var historiqueNettoyages = await _context.PointDechets
                .Where(p => p.Zone == pointDechet.Zone &&
                           p.Statut == StatutDechet.Nettoye &&
                           p.DateNettoyage.HasValue &&
                           p.DateNettoyage.Value >= dateDebut)
                .Select(p => p.DateNettoyage!.Value)
                .ToListAsync();

            var organisationActive = await _context.Organisations
                .Include(o => o.Depots)
                .AnyAsync(o => o.Depots.Any(d =>
                    Math.Abs(d.Latitude - pointDechet.Latitude) < rayonDegres &&
                    Math.Abs(d.Longitude - pointDechet.Longitude) < rayonDegres));


            // Déterminer la saison actuelle
            var saison = DeterminerSaison(DateTime.UtcNow);

            // Estimer le volume si non défini
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

        /// <summary>
        /// Détermine la saison actuelle
        /// </summary>
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

        /// <summary>
        /// Estime le volume d'un déchet en fonction de son type
        /// </summary>
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

        /// <summary>
        /// Génère un message de succès personnalisé
        /// </summary>
        private string GenerateSuccessMessage(
            PointDechet pointDechet, 
            WasteClassificationResponse? classification, 
            RecommandationEcologique? recommandation)
        {
            var messages = new List<string>();

            if (pointDechet.Type.HasValue)
            {
                messages.Add($"Déchet classifié comme {pointDechet.Type}");
            }
            else
            {
                messages.Add("Déchet signalé (classification impossible)");
            }

            if (recommandation != null && recommandation.EstActive)
            {
                messages.Add($"Recommandation générée (urgence: {recommandation.Urgence})");
            }

            return string.Join(" et ", messages) + ".";
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPointDechet(Guid id)
        {
            var point = await _context.PointDechets
                .Include(p => p.User)
                .Include(p => p.Recommandations)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (point == null)
                return NotFound();

            return Ok(point);
        }

        /// <summary>
        /// Récupère les recommandations pour un point de déchet spécifique
        /// </summary>
        [HttpGet("{id}/recommandations")]
        public async Task<IActionResult> GetRecommandations(Guid id)
        {
            var recommandations = await _context.RecommandationsEcologiques
                .Where(r => r.PointDechetId == id && r.EstActive)
                .OrderByDescending(r => r.DateGeneration)
                .ToListAsync();

            if (!recommandations.Any())
                return NotFound(new { message = "Aucune recommandation disponible pour ce déchet" });

            return Ok(recommandations);
        }

        /// <summary>
        /// Force la régénération d'une recommandation pour un point de déchet
        /// </summary>
        [HttpPost("{id}/regenerer-recommandation")]
        public async Task<IActionResult> RegénérerRecommandation(Guid id)
        {
            try
            {
                var pointDechet = await _context.PointDechets.FindAsync(id);
                if (pointDechet == null)
                    return NotFound(new { message = "Point de déchet non trouvé" });

                // Désactiver les anciennes recommandations
                var anciennesRecommandations = await _context.RecommandationsEcologiques
                    .Where(r => r.PointDechetId == id)
                    .ToListAsync();

                foreach (var ancienne in anciennesRecommandations)
                {
                    ancienne.EstActive = false;
                }

                // Générer une nouvelle recommandation
                var contexte = await ConstruireContexteRecommandationAsync(pointDechet);
                var nouvelleRecommandation = await _geminiAgent.GenererRecommandationAsync(contexte);

                if (nouvelleRecommandation != null && nouvelleRecommandation.EstActive)
                {
                    _context.RecommandationsEcologiques.Add(nouvelleRecommandation);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        message = "Recommandation régénérée avec succès",
                        recommandation = nouvelleRecommandation
                    });
                }

                return StatusCode(500, new { message = "Impossible de générer une nouvelle recommandation" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la régénération de recommandation pour {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }

        private async Task<WasteClassificationResponse?> ClassifyWasteAsync(Guid pointDechetId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var classifyUrl = $"{baseUrl}/api/WasteClassification/classify";

                _logger.LogInformation("Appel à l'endpoint de classification: {Url}", classifyUrl);

                var requestBody = new
                {
                    pointDechetId = pointDechetId
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                if (Request.Headers.ContainsKey("Authorization"))
                {
                    client.DefaultRequestHeaders.Add("Authorization", Request.Headers["Authorization"].ToString());
                }

                var response = await client.PostAsync(classifyUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Échec de la classification: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<WasteClassificationResponse>(responseContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'appel à l'API de classification");
                return null;
            }
        }

        private async Task<(string Zone, string Pays)> GetReverseGeocodingAsync(double latitude, double longitude)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "MyEcologicApp/1.0");
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var latStr = latitude.ToString("F6", CultureInfo.InvariantCulture);
            var lonStr = longitude.ToString("F6", CultureInfo.InvariantCulture);

            var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latStr}&lon={lonStr}&zoom=18&addressdetails=1";

            _logger.LogInformation($"Appel Nominatim: {url}");

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Nominatim a retourné le code: {response.StatusCode}");
                throw new Exception($"Nominatim error: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Réponse Nominatim: {json}");

            var data = System.Text.Json.JsonDocument.Parse(json);

            if (!data.RootElement.TryGetProperty("address", out var address))
            {
                _logger.LogWarning("Pas de propriété 'address' dans la réponse Nominatim");
                return ("Inconnue", "Inconnu");
            }

            var zone = address.TryGetProperty("suburb", out var suburb) ? suburb.GetString() :
                       address.TryGetProperty("neighbourhood", out var neighbourhood) ? neighbourhood.GetString() :
                       address.TryGetProperty("quarter", out var quarter) ? quarter.GetString() :
                       address.TryGetProperty("city", out var city) ? city.GetString() :
                       address.TryGetProperty("town", out var town) ? town.GetString() :
                       address.TryGetProperty("village", out var village) ? village.GetString() :
                       address.TryGetProperty("municipality", out var municipality) ? municipality.GetString() : "Inconnue";

            var pays = address.TryGetProperty("country", out var country) ? country.GetString() : "Inconnu";

            _logger.LogInformation($"Résultat geocoding: Zone={zone}, Pays={pays}");

            return (zone ?? "Inconnue", pays ?? "Inconnu");
        }



        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllPointsDechets([FromQuery] PointDechetFilterRequest filter)
        {
            try
            {
                if (filter.Page < 1) filter.Page = 1;
                if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 10;

                var query = _context.PointDechets
                    .Include(p => p.User)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    var searchLower = filter.Search.ToLower();
                    query = query.Where(p =>
                        p.Zone.ToLower().Contains(searchLower) ||
                        p.Pays.ToLower().Contains(searchLower));
                }

                if (filter.Statut.HasValue)
                {
                    query = query.Where(p => p.Statut == filter.Statut.Value);
                }

                if (filter.Type.HasValue)
                {
                    query = query.Where(p => p.Type == filter.Type.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.Pays))
                {
                    query = query.Where(p => p.Pays.ToLower() == filter.Pays.ToLower());
                }

                if (!string.IsNullOrWhiteSpace(filter.Zone))
                {
                    query = query.Where(p => p.Zone.ToLower().Contains(filter.Zone.ToLower()));
                }

                if (filter.DateDebut.HasValue)
                {
                    query = query.Where(p => p.Date >= filter.DateDebut.Value);
                }

                if (filter.DateFin.HasValue)
                {
                    var dateFin = filter.DateFin.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(p => p.Date <= dateFin);
                }

                if (filter.UserId.HasValue)
                {
                    query = query.Where(p => p.UserId == filter.UserId.Value);
                }

                query = filter.SortBy?.ToLower() switch
                {
                    "zone" => filter.Descending
                        ? query.OrderByDescending(p => p.Zone)
                        : query.OrderBy(p => p.Zone),
                    "pays" => filter.Descending
                        ? query.OrderByDescending(p => p.Pays)
                        : query.OrderBy(p => p.Pays),
                    "type" => filter.Descending
                        ? query.OrderByDescending(p => p.Type)
                        : query.OrderBy(p => p.Type),
                    "statut" => filter.Descending
                        ? query.OrderByDescending(p => p.Statut)
                        : query.OrderBy(p => p.Statut),
                    _ => filter.Descending
                        ? query.OrderByDescending(p => p.Date)
                        : query.OrderBy(p => p.Date)
                };

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

                // Pagination
                var items = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(p => new PointDechetResponse
                    {
                        Id = p.Id,
                        Url = p.Url,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        Statut = p.Statut.ToString(),
                        Type = p.Type.HasValue ? p.Type.ToString() : null,
                        Date = p.Date,
                        UserId = p.UserId,
                        UserName = p.User != null ? p.User.Username : null,
                        Zone = p.Zone,
                        Pays = p.Pays,
                        VolumeEstime = p.VolumeEstime
                    })
                    .ToListAsync();

                var result = new PagedResult<PointDechetResponse>
                {
                    Data = items,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = filter.Page > 1,
                    HasNextPage = filter.Page < totalPages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des points de déchets");
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }

        [HttpGet("mes-dechets")]
        public async Task<IActionResult> GetMesDechets([FromQuery] PointDechetFilterRequest filter)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié" });
                }

                if (filter.Page < 1) filter.Page = 1;
                if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 10;

                // Requête filtrée par l'utilisateur connecté
                var query = _context.PointDechets
                    .Include(p => p.User)
                    .Where(p => p.UserId == userId)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    var searchLower = filter.Search.ToLower();
                    query = query.Where(p =>
                        p.Zone.ToLower().Contains(searchLower) ||
                        p.Pays.ToLower().Contains(searchLower));
                }

                if (filter.Statut.HasValue)
                {
                    query = query.Where(p => p.Statut == filter.Statut.Value);
                }

                if (filter.Type.HasValue)
                {
                    query = query.Where(p => p.Type == filter.Type.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.Pays))
                {
                    query = query.Where(p => p.Pays.ToLower() == filter.Pays.ToLower());
                }

                if (!string.IsNullOrWhiteSpace(filter.Zone))
                {
                    query = query.Where(p => p.Zone.ToLower().Contains(filter.Zone.ToLower()));
                }

                if (filter.DateDebut.HasValue)
                {
                    query = query.Where(p => p.Date >= filter.DateDebut.Value);
                }

                if (filter.DateFin.HasValue)
                {
                    var dateFin = filter.DateFin.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(p => p.Date <= dateFin);
                }

                query = filter.SortBy?.ToLower() switch
                {
                    "zone" => filter.Descending
                        ? query.OrderByDescending(p => p.Zone)
                        : query.OrderBy(p => p.Zone),
                    "pays" => filter.Descending
                        ? query.OrderByDescending(p => p.Pays)
                        : query.OrderBy(p => p.Pays),
                    "type" => filter.Descending
                        ? query.OrderByDescending(p => p.Type)
                        : query.OrderBy(p => p.Type),
                    "statut" => filter.Descending
                        ? query.OrderByDescending(p => p.Statut)
                        : query.OrderBy(p => p.Statut),
                    _ => filter.Descending
                        ? query.OrderByDescending(p => p.Date)
                        : query.OrderBy(p => p.Date)
                };

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

                var items = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(p => new PointDechetResponse
                    {
                        Id = p.Id,
                        Url = p.Url,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        Statut = p.Statut.ToString(),
                        Type = p.Type.HasValue ? p.Type.ToString() : null,
                        Date = p.Date,
                        UserId = p.UserId,
                        UserName = p.User != null ? p.User.Username : null,
                        Zone = p.Zone,
                        Pays = p.Pays,
                        VolumeEstime = p.VolumeEstime
                    })
                    .ToListAsync();

                var result = new PagedResult<PointDechetResponse>
                {
                    Data = items,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = filter.Page > 1,
                    HasNextPage = filter.Page < totalPages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de mes points de déchets");
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }


        [HttpGet("statistiques")]
        //[Authorize]
        public async Task<IActionResult> GetStatistiques([FromQuery] StatistiquesRequest request)
        {
            try
            {
                var query = _context.PointDechets
                    .Include(p => p.User)
                    .AsQueryable();

                // Apply filters
                if (request.DateDebut.HasValue)
                {
                    var dateDebutUtc = DateTime.SpecifyKind(request.DateDebut.Value, DateTimeKind.Utc);
                    query = query.Where(p => p.Date >= dateDebutUtc);
                }

                if (request.DateFin.HasValue)
                {
                    var dateFin = request.DateFin.Value.Date.AddDays(1).AddTicks(-1);
                    var dateFinUtc = DateTime.SpecifyKind(dateFin, DateTimeKind.Utc);
                    query = query.Where(p => p.Date <= dateFinUtc);
                }

                if (!string.IsNullOrWhiteSpace(request.Pays))
                {
                    query = query.Where(p => p.Pays.ToLower() == request.Pays.ToLower());
                }

                if (!string.IsNullOrWhiteSpace(request.Zone))
                {
                    query = query.Where(p => p.Zone.ToLower().Contains(request.Zone.ToLower()));
                }

                if (request.UserId.HasValue)
                {
                    query = query.Where(p => p.UserId == request.UserId.Value);
                }

                var dechets = await query.ToListAsync();
                var response = CalculerStatistiques(dechets);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques");
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }

        [HttpGet("mes-statistiques")]
        public async Task<IActionResult> GetMesStatistiques([FromQuery] DateTime? dateDebut, [FromQuery] DateTime? dateFin)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié" });
                }

                var request = new StatistiquesRequest
                {
                    UserId = userId,
                    DateDebut = dateDebut,
                    DateFin = dateFin
                };

                return await GetStatistiques(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de mes statistiques");
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }

        
        [HttpGet("statistiques/telecharger")]
        //[Authorize]
        public async Task<IActionResult> TelechargerStatistiques(
            [FromQuery] StatistiquesRequest request,
            [FromQuery] string format = "csv")
        {
            try
            {
                var query = _context.PointDechets
                    .Include(p => p.User)
                    .AsQueryable();

                if (request.DateDebut.HasValue)
                {
                    var dateDebutUtc = DateTime.SpecifyKind(request.DateDebut.Value, DateTimeKind.Utc);
                    query = query.Where(p => p.Date >= dateDebutUtc);
                }

                if (request.DateFin.HasValue)
                {
                    var dateFin = request.DateFin.Value.Date.AddDays(1).AddTicks(-1);
                    var dateFinUtc = DateTime.SpecifyKind(dateFin, DateTimeKind.Utc);
                    query = query.Where(p => p.Date <= dateFinUtc);
                }

                if (!string.IsNullOrWhiteSpace(request.Pays))
                {
                    query = query.Where(p => p.Pays.ToLower() == request.Pays.ToLower());
                }

                if (!string.IsNullOrWhiteSpace(request.Zone))
                {
                    query = query.Where(p => p.Zone.ToLower().Contains(request.Zone.ToLower()));
                }

                if (request.UserId.HasValue)
                {
                    query = query.Where(p => p.UserId == request.UserId.Value);
                }

                var dechets = await query.ToListAsync();
                var stats = CalculerStatistiques(dechets);

                format = format.ToLower();
                switch (format)
                {
                    case "csv":
                        return GenerateCSV(stats);
                    case "json":
                        return GenerateJSON(stats);
                    case "excel":
                        return GenerateExcel(stats);
                    default:
                        return BadRequest(new { message = "Format non supporté. Utilisez: csv, json, ou excel" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement des statistiques");
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }

        [HttpGet("mes-statistiques/telecharger")]
        [Authorize]
        public async Task<IActionResult> TelechargerMesStatistiques(
            [FromQuery] DateTime? dateDebut,
            [FromQuery] DateTime? dateFin,
            [FromQuery] string format = "csv")
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié" });
                }

                var request = new StatistiquesRequest
                {
                    UserId = userId,
                    DateDebut = dateDebut,
                    DateFin = dateFin
                };

                return await TelechargerStatistiques(request, format);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement de mes statistiques");
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }

        private StatistiquesResponse CalculerStatistiques(List<PointDechet> dechets)
        {
            var totalDechets = dechets.Count;

            if (totalDechets == 0)
            {
                return new StatistiquesResponse
                {
                    TotalDechets = 0,
                    TotalSignales = 0,
                    TotalNettoyés = 0,
                    PourcentageNettoyage = 0
                };
            }

            var totalSignales = dechets.Count(d => d.Statut == StatutDechet.Signale);
            var totalNettoyés = dechets.Count(d => d.Statut == StatutDechet.Nettoye);
            var pourcentageNettoyage = totalDechets > 0 ? (double)totalNettoyés / totalDechets * 100 : 0;

            var parStatut = dechets
                .GroupBy(d => d.Statut)
                .Select(g => new StatParStatut
                {
                    Statut = g.Key.ToString(),
                    Nombre = g.Count(),
                    Pourcentage = (double)g.Count() / totalDechets * 100
                })
                .OrderByDescending(s => s.Nombre)
                .ToList();

            var parType = dechets
                .Where(d => d.Type.HasValue)
                .GroupBy(d => d.Type!.Value)
                .Select(g => new StatParType
                {
                    Type = g.Key.ToString(),
                    Nombre = g.Count(),
                    Pourcentage = (double)g.Count() / totalDechets * 100
                })
                .OrderByDescending(s => s.Nombre)
                .ToList();

            var sansType = dechets.Count(d => !d.Type.HasValue);
            if (sansType > 0)
            {
                parType.Add(new StatParType
                {
                    Type = "Non classifié",
                    Nombre = sansType,
                    Pourcentage = (double)sansType / totalDechets * 100
                });
            }

            var parZone = dechets
                .GroupBy(d => d.Zone)
                .Select(g => new StatParZone
                {
                    Zone = g.Key,
                    Nombre = g.Count(),
                    Signales = g.Count(d => d.Statut == StatutDechet.Signale),
                    Nettoyés = g.Count(d => d.Statut == StatutDechet.Nettoye),
                    PourcentageNettoyage = g.Count() > 0
                        ? (double)g.Count(d => d.Statut == StatutDechet.Nettoye) / g.Count() * 100
                        : 0
                })
                .OrderByDescending(s => s.Nombre)
                .Take(10)
                .ToList();

            var parPays = dechets
                .GroupBy(d => d.Pays)
                .Select(g => new StatParPays
                {
                    Pays = g.Key,
                    Nombre = g.Count(),
                    Signales = g.Count(d => d.Statut == StatutDechet.Signale),
                    Nettoyés = g.Count(d => d.Statut == StatutDechet.Nettoye),
                    PourcentageNettoyage = g.Count() > 0
                        ? (double)g.Count(d => d.Statut == StatutDechet.Nettoye) / g.Count() * 100
                        : 0
                })
                .OrderByDescending(s => s.Nombre)
                .ToList();

            var parDate = dechets
                .GroupBy(d => d.Date.Date)
                .Select(g => new StatParDate
                {
                    Date = g.Key,
                    Nombre = g.Count(),
                    Signales = g.Count(d => d.Statut == StatutDechet.Signale),
                    Nettoyés = g.Count(d => d.Statut == StatutDechet.Nettoye)
                })
                .OrderBy(s => s.Date)
                .ToList();

            var topUtilisateurs = dechets
                .GroupBy(d => new { d.UserId, UserName = d.User != null ? d.User.Username : "Inconnu" })
                .Select(g => new StatParUtilisateur
                {
                    UserId = g.Key.UserId,
                    UserName = g.Key.UserName,
                    NombreDechetsSignales = g.Count(d => d.Statut == StatutDechet.Signale),
                    NombreDechetsNettoyés = g.Count(d => d.Statut == StatutDechet.Nettoye)
                })
                .OrderByDescending(s => s.NombreDechetsSignales + s.NombreDechetsNettoyés)
                .Take(10)
                .ToList();

            return new StatistiquesResponse
            {
                TotalDechets = totalDechets,
                TotalSignales = totalSignales,
                TotalNettoyés = totalNettoyés,
                PourcentageNettoyage = Math.Round(pourcentageNettoyage, 2),
                ParStatut = parStatut,
                ParType = parType,
                ParZone = parZone,
                ParPays = parPays,
                ParDate = parDate,
                TopUtilisateurs = topUtilisateurs
            };
        }

        private IActionResult GenerateCSV(StatistiquesResponse stats)
        {
            var csv = new StringBuilder();
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            csv.AppendLine("=== RÉSUMÉ GÉNÉRAL ===");
            csv.AppendLine($"Total Déchets,{stats.TotalDechets}");
            csv.AppendLine($"Total Signalés,{stats.TotalSignales}");
            csv.AppendLine($"Total Nettoyés,{stats.TotalNettoyés}");
            csv.AppendLine($"Pourcentage Nettoyage,{stats.PourcentageNettoyage}%");
            csv.AppendLine();

            csv.AppendLine("=== STATISTIQUES PAR STATUT ===");
            csv.AppendLine("Statut,Nombre,Pourcentage");
            foreach (var stat in stats.ParStatut)
            {
                csv.AppendLine($"{stat.Statut},{stat.Nombre},{stat.Pourcentage:F2}%");
            }
            csv.AppendLine();

            csv.AppendLine("=== STATISTIQUES PAR TYPE ===");
            csv.AppendLine("Type,Nombre,Pourcentage");
            foreach (var stat in stats.ParType)
            {
                csv.AppendLine($"{stat.Type},{stat.Nombre},{stat.Pourcentage:F2}%");
            }
            csv.AppendLine();

            csv.AppendLine("=== STATISTIQUES PAR ZONE ===");
            csv.AppendLine("Zone,Nombre Total,Signalés,Nettoyés,Pourcentage Nettoyage");
            foreach (var stat in stats.ParZone)
            {
                csv.AppendLine($"{stat.Zone},{stat.Nombre},{stat.Signales},{stat.Nettoyés},{stat.PourcentageNettoyage:F2}%");
            }
            csv.AppendLine();

            csv.AppendLine("=== STATISTIQUES PAR PAYS ===");
            csv.AppendLine("Pays,Nombre Total,Signalés,Nettoyés,Pourcentage Nettoyage");
            foreach (var stat in stats.ParPays)
            {
                csv.AppendLine($"{stat.Pays},{stat.Nombre},{stat.Signales},{stat.Nettoyés},{stat.PourcentageNettoyage:F2}%");
            }
            csv.AppendLine();

            csv.AppendLine("=== STATISTIQUES PAR DATE ===");
            csv.AppendLine("Date,Nombre Total,Signalés,Nettoyés");
            foreach (var stat in stats.ParDate)
            {
                csv.AppendLine($"{stat.Date:yyyy-MM-dd},{stat.Nombre},{stat.Signales},{stat.Nettoyés}");
            }
            csv.AppendLine();

            csv.AppendLine("=== TOP UTILISATEURS ===");
            csv.AppendLine("Utilisateur,Déchets Signalés,Déchets Nettoyés,Total");
            foreach (var stat in stats.TopUtilisateurs)
            {
                var total = stat.NombreDechetsSignales + stat.NombreDechetsNettoyés;
                csv.AppendLine($"{stat.UserName},{stat.NombreDechetsSignales},{stat.NombreDechetsNettoyés},{total}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"statistiques_{timestamp}.csv");
        }

        private IActionResult GenerateJSON(StatistiquesResponse stats)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(stats, options);
            var bytes = Encoding.UTF8.GetBytes(json);

            return File(bytes, "application/json", $"statistiques_{timestamp}.json");
        }

        private IActionResult GenerateExcel(StatistiquesResponse stats)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var csv = new StringBuilder();

            csv.AppendLine("RÉSUMÉ GÉNÉRAL");
            csv.AppendLine("Métrique,Valeur");
            csv.AppendLine($"Total Déchets,{stats.TotalDechets}");
            csv.AppendLine($"Total Signalés,{stats.TotalSignales}");
            csv.AppendLine($"Total Nettoyés,{stats.TotalNettoyés}");
            csv.AppendLine($"Pourcentage Nettoyage,{stats.PourcentageNettoyage}%");
            csv.AppendLine();
            csv.AppendLine();

            csv.AppendLine("STATISTIQUES PAR STATUT");
            csv.AppendLine("Statut,Nombre,Pourcentage");
            foreach (var stat in stats.ParStatut)
            {
                csv.AppendLine($"{stat.Statut},{stat.Nombre},{stat.Pourcentage:F2}");
            }
            csv.AppendLine();
            csv.AppendLine();

            csv.AppendLine("STATISTIQUES PAR TYPE");
            csv.AppendLine("Type,Nombre,Pourcentage");
            foreach (var stat in stats.ParType)
            {
                csv.AppendLine($"{stat.Type},{stat.Nombre},{stat.Pourcentage:F2}");
            }
            csv.AppendLine();
            csv.AppendLine();

            csv.AppendLine("STATISTIQUES PAR ZONE");
            csv.AppendLine("Zone,Nombre Total,Signalés,Nettoyés,Pourcentage Nettoyage");
            foreach (var stat in stats.ParZone)
            {
                csv.AppendLine($"{stat.Zone},{stat.Nombre},{stat.Signales},{stat.Nettoyés},{stat.PourcentageNettoyage:F2}");
            }
            csv.AppendLine();
            csv.AppendLine();

            csv.AppendLine("STATISTIQUES PAR PAYS");
            csv.AppendLine("Pays,Nombre Total,Signalés,Nettoyés,Pourcentage Nettoyage");
            foreach (var stat in stats.ParPays)
            {
                csv.AppendLine($"{stat.Pays},{stat.Nombre},{stat.Signales},{stat.Nettoyés},{stat.PourcentageNettoyage:F2}");
            }
            csv.AppendLine();
            csv.AppendLine();

            csv.AppendLine("STATISTIQUES PAR DATE");
            csv.AppendLine("Date,Nombre Total,Signalés,Nettoyés");
            foreach (var stat in stats.ParDate)
            {
                csv.AppendLine($"{stat.Date:yyyy-MM-dd},{stat.Nombre},{stat.Signales},{stat.Nettoyés}");
            }
            csv.AppendLine();
            csv.AppendLine();

            csv.AppendLine("TOP UTILISATEURS");
            csv.AppendLine("Utilisateur,Déchets Signalés,Déchets Nettoyés,Total");
            foreach (var stat in stats.TopUtilisateurs)
            {
                var total = stat.NombreDechetsSignales + stat.NombreDechetsNettoyés;
                csv.AppendLine($"{stat.UserName},{stat.NombreDechetsSignales},{stat.NombreDechetsNettoyés},{total}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            
            return File(bytes, "application/vnd.ms-excel", $"statistiques_{timestamp}.xls");
        }
    
    }
}