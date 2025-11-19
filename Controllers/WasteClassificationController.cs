using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.EntityFrameworkCore;

namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WasteClassificationController : ControllerBase
    {
        private readonly ILogger<WasteClassificationController> _logger;
        private readonly RoboflowSettings _roboflowSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly EcologicDbContext _context;
        private readonly IWebHostEnvironment _env;

        private const int MAX_IMAGE_SIZE_MB = 10;
        private const int MAX_IMAGE_SIZE_BYTES = MAX_IMAGE_SIZE_MB * 1024 * 1024;
        private const int MAX_BATCH_SIZE = 10;
        private const int RATE_LIMIT_DELAY_MS = 500;
        private const int CACHE_DURATION_HOURS = 24;
        private const int COMPRESSED_IMAGE_MAX_WIDTH = 1024;
        private const int COMPRESSED_IMAGE_MAX_HEIGHT = 1024;
        private const int JPEG_QUALITY = 85;
        private const double MIN_VALID_CONFIDENCE = 0.1; // Seuil minimum pour considérer un résultat valide

        public WasteClassificationController(
            ILogger<WasteClassificationController> logger,
            IOptions<RoboflowSettings> roboflowSettings,
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            EcologicDbContext context,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _roboflowSettings = roboflowSettings.Value;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _context = context;
            _env = env;
        }

        [HttpPost("classify")]
        [ProducesResponseType(typeof(WasteClassificationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ClassifyWaste([FromBody] ClassifyWasteRequest request)
        {
            try
            {
                if (request.PointDechetId == Guid.Empty)
                {
                    return BadRequest(new { error = "ID du point de déchet invalide" });
                }

                var pointDechet = await _context.PointDechets.FindAsync(request.PointDechetId);

                if (pointDechet == null)
                {
                    return NotFound(new { error = "Point de déchet non trouvé" });
                }

                _logger.LogInformation("Classification du déchet {Id} avec l'image {Url}",
                    pointDechet.Id, pointDechet.Url);

                var imagePath = Path.Combine(_env.WebRootPath, pointDechet.Url.TrimStart('/'));

                if (!System.IO.File.Exists(imagePath))
                {
                    _logger.LogError("Image non trouvée: {Path}", imagePath);
                    return BadRequest(new { error = "Image non trouvée sur le serveur" });
                }

                byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
                _logger.LogInformation("Image chargée: {Size} bytes", imageBytes.Length);

                if (imageBytes.Length > 500 * 1024)
                {
                    _logger.LogInformation("Compression de l'image de {Original}KB", imageBytes.Length / 1024);
                    imageBytes = await CompressImageAsync(imageBytes);
                    _logger.LogInformation("Image compressée à {Compressed}KB", imageBytes.Length / 1024);
                }

                var imageHash = ComputeHash(imageBytes);
                var cacheKey = $"waste_classification_{imageHash}";

                WasteClassificationResponse result;
                bool usedCache = false;

                if (_cache.TryGetValue<WasteClassificationResponse>(cacheKey, out var cachedResult) && 
                    cachedResult != null && 
                    IsValidCachedResult(cachedResult))
                {
                    _logger.LogInformation("Résultat VALIDE récupéré du cache pour {Hash}", imageHash);
                    result = cachedResult;
                    result.FromCache = true;
                    usedCache = true;
                }
                else
                {
                    if (cachedResult != null && !IsValidCachedResult(cachedResult))
                    {
                        _logger.LogWarning("Résultat en cache INVALIDE (confidence: {Confidence}), appel à l'API Roboflow", 
                            cachedResult.Confidence);
                        _cache.Remove(cacheKey); 
                    }

                    _logger.LogInformation("Appel à l'API Roboflow pour classification");
                    var roboflowResponse = await CallRoboflowAPI(imageBytes, "image/jpeg");
                    
                    result = MapToWasteCategories(roboflowResponse);
                    result.FromCache = false;

                    if (IsValidCachedResult(result))
                    {
                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromHours(CACHE_DURATION_HOURS))
                            .SetSize(1);
                        _cache.Set(cacheKey, result, cacheOptions);
                        _logger.LogInformation("Résultat mis en cache avec confidence {Confidence}", result.Confidence);
                    }
                    else
                    {
                        _logger.LogWarning("Résultat non mis en cache car confidence trop faible: {Confidence}", 
                            result.Confidence);
                    }
                }

                if (result.Success && 
                    !string.IsNullOrEmpty(result.Category) && 
                    result.Confidence > MIN_VALID_CONFIDENCE)
                {
                    TypeDechet typeDechet = MapCategoryToTypeDechet(result.Category);
                    pointDechet.Type = typeDechet;

                    _context.PointDechets.Update(pointDechet);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Type de déchet mis à jour: {Type} pour le point {Id} (confidence: {Confidence})",
                        typeDechet, pointDechet.Id, result.Confidence);
                }
                else
                {
                    _logger.LogWarning("Type de déchet NON mis à jour pour {Id} - Confidence trop faible ou erreur: {Confidence}", 
                        pointDechet.Id, result.Confidence);
                }

                result.FileName = Path.GetFileName(pointDechet.Url);

                _logger.LogInformation("Classification terminée: {Category} avec {Confidence}% de confiance (cache: {Cache})",
                    result.Category, result.Confidence * 100, usedCache);

                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erreur lors de l'appel à l'API Roboflow");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { error = "Service de classification temporairement indisponible", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la classification de l'image");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Erreur interne du serveur", details = ex.Message });
            }
        }

        private bool IsValidCachedResult(WasteClassificationResponse cachedResult)
        {
            return cachedResult.Success && 
                   !string.IsNullOrEmpty(cachedResult.Category) && 
                   cachedResult.Confidence >= MIN_VALID_CONFIDENCE;
        }

        [HttpPost("clear-cache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult ClearCache([FromQuery] string? imageHash = null)
        {
            if (!string.IsNullOrEmpty(imageHash))
            {
                var cacheKey = $"waste_classification_{imageHash}";
                _cache.Remove(cacheKey);
                _logger.LogInformation("Cache vidé pour l'image: {Hash}", imageHash);
                return Ok(new { message = $"Cache vidé pour l'image {imageHash}" });
            }
            else
            {
                _logger.LogInformation("Demande de vidage complet du cache");
                return Ok(new { message = "Pour vider le cache complet, redémarrez l'application. Pour vider une image spécifique, passez le paramètre imageHash." });
            }
        }

        private ValidationError? ValidateImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return new ValidationError { Error = "Aucune image fournie" };
            }

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
            if (!allowedTypes.Contains(image.ContentType.ToLower()))
            {
                return new ValidationError { Error = "Format d'image non supporté. Utilisez JPG ou PNG." };
            }

            if (image.Length > MAX_IMAGE_SIZE_BYTES)
            {
                return new ValidationError { Error = $"L'image ne doit pas dépasser {MAX_IMAGE_SIZE_MB}MB" };
            }

            return null;
        }

        private class ValidationError
        {
            public string Error { get; set; } = string.Empty;
        }

        private async Task<byte[]> CompressImageAsync(byte[] imageBytes)
        {
            try
            {
                using var inputStream = new MemoryStream(imageBytes);
                using var image = await Image.LoadAsync(inputStream);

                if (image.Width > COMPRESSED_IMAGE_MAX_WIDTH || image.Height > COMPRESSED_IMAGE_MAX_HEIGHT)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(COMPRESSED_IMAGE_MAX_WIDTH, COMPRESSED_IMAGE_MAX_HEIGHT),
                        Mode = ResizeMode.Max
                    }));
                }

                using var outputStream = new MemoryStream();
                var encoder = new JpegEncoder { Quality = JPEG_QUALITY };
                await image.SaveAsync(outputStream, encoder);

                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Échec de la compression, utilisation de l'image originale");
                return imageBytes;
            }
        }

        private string ComputeHash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(data);
            return Convert.ToBase64String(hashBytes);
        }

        private async Task<RoboflowApiResponse> CallRoboflowAPI(byte[] imageBytes, string contentType, int retryCount = 0)
        {
            const int maxRetries = 2;

            try
            {
                var client = _httpClientFactory.CreateClient("Roboflow");
                client.Timeout = TimeSpan.FromSeconds(30); // Augmenter le timeout

                if (string.IsNullOrEmpty(_roboflowSettings.ApiKey))
                {
                    throw new InvalidOperationException("Roboflow API Key is not configured");
                }

                if (string.IsNullOrEmpty(_roboflowSettings.ModelId))
                {
                    throw new InvalidOperationException("Roboflow Model ID is not configured");
                }

                // Construire l'URL complète de l'API
                string apiUrl = $"https://detect.roboflow.com/{_roboflowSettings.ModelId}/{_roboflowSettings.Version}";
                string confidenceParam = _roboflowSettings.ConfidenceThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string url = $"{apiUrl}?api_key={_roboflowSettings.ApiKey}&confidence={confidenceParam}";

                _logger.LogInformation("Appel Roboflow API: {Model} (confidence: {Conf})", 
                    _roboflowSettings.ModelId, confidenceParam);

                string base64Image = Convert.ToBase64String(imageBytes);
                using var content = new StringContent(base64Image, Encoding.UTF8, "text/plain");

                var response = await client.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Réponse Roboflow: Status={Status}, Content={Content}", 
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Erreur API Roboflow: {StatusCode} - {Error}",
                        response.StatusCode, responseContent);

                    if (response.StatusCode >= System.Net.HttpStatusCode.InternalServerError && retryCount < maxRetries)
                    {
                        await Task.Delay(1000 * (retryCount + 1));
                        return await CallRoboflowAPI(imageBytes, contentType, retryCount + 1);
                    }

                    throw new HttpRequestException($"Erreur API Roboflow: {response.StatusCode} - {responseContent}");
                }

                var result = JsonSerializer.Deserialize<RoboflowApiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    throw new HttpRequestException("Impossible de désérialiser la réponse");
                }

                return result;
            }
            catch (Exception ex) when (retryCount < maxRetries && !(ex is InvalidOperationException))
            {
                _logger.LogWarning(ex, "Erreur lors de l'appel API, retry {Retry}/{Max}", retryCount + 1, maxRetries);
                await Task.Delay(1000 * (retryCount + 1));
                return await CallRoboflowAPI(imageBytes, contentType, retryCount + 1);
            }
        }

        private WasteClassificationResponse MapToWasteCategories(RoboflowApiResponse roboflowResponse)
        {
            if (roboflowResponse?.Predictions == null || roboflowResponse.Predictions.Count == 0)
            {
                _logger.LogWarning("Aucune prédiction retournée par Roboflow");
                return new WasteClassificationResponse
                {
                    Category = "Autre",
                    Confidence = 0.0,
                    Success = true,
                    Message = "Aucun déchet détecté dans l'image. Assurez-vous que l'image contient un déchet visible et de bonne qualité."
                };
            }

            var bestPrediction = roboflowResponse.Predictions
                .OrderByDescending(p => p.Confidence)
                .First();

            _logger.LogInformation("Meilleure prédiction: {Class} avec {Confidence}% de confiance", 
                bestPrediction.Class, bestPrediction.Confidence * 100);

            var categoryMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Plastics", "Plastique" },
                { "bottles", "Plastique" },
                { "plastic-bags", "Plastique" },
                { "plastic-items", "Plastique" },
                { "styrofoam", "Plastique" },
                { "glass", "Verre" },
                { "cans", "Metale" },
                { "metal", "Metale" },
                { "spoons", "Metale" },
                { "e-waste", "Pile" },
                { "cables", "Pile" },
                { "paper", "Papier" },
                { "carton", "Papier" },
                { "organic-waste", "Autre" },
                { "phone-cases", "Autre" },
                { "wood-waste", "Autre" },
                { "yoga-mats", "Autre" },
                { "trash", "Autre" }
            };

            string mappedCategory = categoryMapping.TryGetValue(bestPrediction.Class, out var category)
                ? category
                : "Autre";

            return new WasteClassificationResponse
            {
                Category = mappedCategory,
                Confidence = bestPrediction.Confidence,
                RoboflowClass = bestPrediction.Class,
                DetectedObjects = roboflowResponse.Predictions.Count,
                ProcessingTime = roboflowResponse.Time,
                Success = true,
                AllDetections = roboflowResponse.Predictions.Select(p => new Detection
                {
                    Class = p.Class,
                    Confidence = p.Confidence,
                    X = p.X,
                    Y = p.Y,
                    Width = p.Width,
                    Height = p.Height
                }).ToList()
            };
        }

        private TypeDechet MapCategoryToTypeDechet(string category)
        {
            return category switch
            {
                "Plastique" => TypeDechet.Plastique,
                "Verre" => TypeDechet.Verre,
                "Metale" => TypeDechet.Metale,
                "Pile" => TypeDechet.Pile,
                "Papier" => TypeDechet.Papier,
                _ => TypeDechet.Autre
            };
        }

        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult HealthCheck()
        {
            var health = new
            {
                status = "healthy",
                service = "Roboflow Waste Detection",
                apiConfigured = !string.IsNullOrEmpty(_roboflowSettings.ApiKey),
                modelId = _roboflowSettings.ModelId,
                confidenceThreshold = _roboflowSettings.ConfidenceThreshold,
                cacheEnabled = true,
                timestamp = DateTime.UtcNow
            };

            return Ok(health);
        }
    }

    public class ClassifyWasteRequest
    {
        public Guid PointDechetId { get; set; }
    }
}