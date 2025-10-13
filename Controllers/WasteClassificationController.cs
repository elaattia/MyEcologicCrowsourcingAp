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

        /// <summary>
        /// Classifie un déchet déjà signalé par son ID et met à jour le type dans la base de données
        /// </summary>
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

                // Récupérer le point de déchet depuis la base de données
                var pointDechet = await _context.PointDechets.FindAsync(request.PointDechetId);

                if (pointDechet == null)
                {
                    return NotFound(new { error = "Point de déchet non trouvé" });
                }

                _logger.LogInformation("Classification du déchet {Id} avec l'image {Url}",
                    pointDechet.Id, pointDechet.Url);

                // Construire le chemin complet de l'image
                var imagePath = Path.Combine(_env.WebRootPath, pointDechet.Url.TrimStart('/'));

                if (!System.IO.File.Exists(imagePath))
                {
                    return BadRequest(new { error = "Image non trouvée sur le serveur" });
                }

                // Lire l'image depuis le disque
                byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);

                _logger.LogInformation("Image chargée: {Size} bytes", imageBytes.Length);

                // Compression si nécessaire
                if (imageBytes.Length > 500 * 1024) // 500KB
                {
                    _logger.LogInformation("Compression de l'image de {Original}KB",
                        imageBytes.Length / 1024);
                    imageBytes = await CompressImageAsync(imageBytes);
                    _logger.LogInformation("Image compressée à {Compressed}KB",
                        imageBytes.Length / 1024);
                }

                // Vérifier le cache
                var imageHash = ComputeHash(imageBytes);
                var cacheKey = $"waste_classification_{imageHash}";

                WasteClassificationResponse result;

                if (_cache.TryGetValue<WasteClassificationResponse>(cacheKey, out var cachedResult) && cachedResult != null)
                {
                    _logger.LogInformation("Résultat récupéré du cache pour {Hash}", imageHash);
                    result = cachedResult;
                    result.FromCache = true;
                }
                else
                {
                    // Appeler l'API Roboflow
                    var roboflowResponse = await CallRoboflowAPI(imageBytes, "image/jpeg");
                    result = MapToWasteCategories(roboflowResponse);
                    result.FromCache = false;

                    // Mettre en cache
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromHours(CACHE_DURATION_HOURS))
                        .SetSize(1);
                    _cache.Set(cacheKey, result, cacheOptions);
                }

                // Mettre à jour le type de déchet dans la base de données
                if (result.Success && !string.IsNullOrEmpty(result.Category))
                {
                    TypeDechet typeDechet = MapCategoryToTypeDechet(result.Category);
                    pointDechet.Type = typeDechet;

                    _context.PointDechets.Update(pointDechet);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Type de déchet mis à jour: {Type} pour le point {Id}",
                        typeDechet, pointDechet.Id);
                }

                result.FileName = Path.GetFileName(pointDechet.Url);

                _logger.LogInformation("Classification réussie: {Category} avec {Confidence}% de confiance",
                    result.Category, result.Confidence * 100);

                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erreur lors de l'appel à l'API Roboflow");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { error = "Service de classification temporairement indisponible" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la classification de l'image");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Erreur interne du serveur", details = ex.Message });
            }
        }

        /// <summary>
        /// Ancien endpoint de classification avec upload d'image (conservé pour compatibilité)
        /// </summary>
        [HttpPost("classify-upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(WasteClassificationResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClassifyWasteUpload([FromForm] IFormFile image)
        {
            try
            {
                var validationError = ValidateImage(image);
                if (validationError != null)
                    return BadRequest(new { error = validationError.Error });

                _logger.LogInformation("Classification d'une image uploadée: {FileName}, Taille: {Size} bytes",
                    image.FileName, image.Length);

                byte[] imageBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await image.CopyToAsync(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }

                if (imageBytes.Length > 500 * 1024)
                {
                    imageBytes = await CompressImageAsync(imageBytes);
                }

                var imageHash = ComputeHash(imageBytes);
                var cacheKey = $"waste_classification_{imageHash}";

                if (_cache.TryGetValue<WasteClassificationResponse>(cacheKey, out var cachedResult) && cachedResult != null)
                {
                    cachedResult.FromCache = true;
                    return Ok(cachedResult);
                }

                var roboflowResponse = await CallRoboflowAPI(imageBytes, image.ContentType);
                var result = MapToWasteCategories(roboflowResponse);
                result.FromCache = false;
                result.FileName = image.FileName;

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(CACHE_DURATION_HOURS))
                    .SetSize(1);
                _cache.Set(cacheKey, result, cacheOptions);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la classification");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Erreur interne du serveur" });
            }
        }

        [HttpPost("classify-batch")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(BatchClassificationResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClassifyWasteBatch([FromForm] List<IFormFile> images)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                if (images == null || images.Count == 0)
                {
                    return BadRequest(new { error = "Aucune image fournie" });
                }

                if (images.Count > MAX_BATCH_SIZE)
                {
                    return BadRequest(new { error = $"Maximum {MAX_BATCH_SIZE} images par batch" });
                }

                _logger.LogInformation("Classification batch de {Count} images", images.Count);

                var results = new List<WasteClassificationResponse>();
                var semaphore = new SemaphoreSlim(3, 3);

                var tasks = images.Select(async image =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var validationError = ValidateImage(image);
                        if (validationError != null)
                        {
                            return new WasteClassificationResponse
                            {
                                FileName = image.FileName ?? "unknown",
                                Success = false,
                                ErrorMessage = validationError.Error
                            };
                        }

                        byte[] imageBytes;
                        using (var memoryStream = new MemoryStream())
                        {
                            await image.CopyToAsync(memoryStream);
                            imageBytes = memoryStream.ToArray();
                        }

                        if (imageBytes.Length > 500 * 1024)
                        {
                            imageBytes = await CompressImageAsync(imageBytes);
                        }

                        var imageHash = ComputeHash(imageBytes);
                        var cacheKey = $"waste_classification_{imageHash}";

                        if (_cache.TryGetValue<WasteClassificationResponse>(cacheKey, out var cachedResult) && cachedResult != null)
                        {
                            cachedResult.FileName = image.FileName ?? "unknown";
                            cachedResult.FromCache = true;
                            return cachedResult;
                        }

                        var roboflowResponse = await CallRoboflowAPI(imageBytes, image.ContentType);
                        var result = MapToWasteCategories(roboflowResponse);
                        result.FileName = image.FileName;
                        result.FromCache = false;

                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromHours(CACHE_DURATION_HOURS))
                            .SetSize(1);
                        _cache.Set(cacheKey, result, cacheOptions);

                        await Task.Delay(RATE_LIMIT_DELAY_MS);

                        return result;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de la classification de {FileName}", image.FileName);
                        return new WasteClassificationResponse
                        {
                            FileName = image.FileName,
                            Success = false,
                            ErrorMessage = ex.Message
                        };
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                results = (await Task.WhenAll(tasks)).ToList();

                var endTime = DateTime.UtcNow;
                var totalTime = (endTime - startTime).TotalSeconds;

                var stats = new BatchClassificationResponse
                {
                    TotalImages = images.Count,
                    SuccessCount = results.Count(r => r.Success),
                    FailureCount = results.Count(r => !r.Success),
                    CachedCount = results.Count(r => r.FromCache),
                    TotalProcessingTime = totalTime,
                    Results = results,
                    CategoryStatistics = results
                        .Where(r => r.Success)
                        .GroupBy(r => r.Category)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                _logger.LogInformation("Batch terminé: {Success}/{Total} réussies en {Time}s",
                    stats.SuccessCount, stats.TotalImages, totalTime);

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la classification batch");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Erreur interne du serveur" });
            }
        }

        [HttpGet("cache-stats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetCacheStatistics()
        {
            var stats = new
            {
                cacheEnabled = true,
                cacheDuration = $"{CACHE_DURATION_HOURS} hours",
                message = "Le cache est actif. Les images identiques sont servies depuis le cache."
            };

            return Ok(stats);
        }

        [HttpPost("clear-cache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult ClearCache()
        {
            _logger.LogInformation("Demande de vidage du cache");
            return Ok(new { message = "Cache vidé (les entrées expireront naturellement)" });
        }

        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<CategoryInfo>), StatusCodes.Status200OK)]
        public IActionResult GetCategories()
        {
            var categories = new List<CategoryInfo>
            {
                new CategoryInfo
                {
                    Name = "Plastique",
                    Description = "Bouteilles, sacs, objets en plastique",
                    RoboflowClasses = new[] { "bottles", "plastic-bags", "plastic-items", "styrofoam" },
                    RecyclingInfo = "Recyclable dans la poubelle jaune"
                },
                new CategoryInfo
                {
                    Name = "Verre",
                    Description = "Bouteilles et objets en verre",
                    RoboflowClasses = new[] { "glass" },
                    RecyclingInfo = "Recyclable dans les conteneurs à verre"
                },
                new CategoryInfo
                {
                    Name = "Metale",
                    Description = "Canettes, objets métalliques",
                    RoboflowClasses = new[] { "cans", "metal", "spoons" },
                    RecyclingInfo = "Recyclable dans la poubelle jaune"
                },
                new CategoryInfo
                {
                    Name = "Pile",
                    Description = "Batteries, piles, câbles, déchets électroniques",
                    RoboflowClasses = new[] { "e-waste", "cables" },
                    RecyclingInfo = "À déposer dans les points de collecte spécialisés"
                },
                new CategoryInfo
                {
                    Name = "Papier",
                    Description = "Papier, carton",
                    RoboflowClasses = new[] { "paper", "carton" },
                    RecyclingInfo = "Recyclable dans la poubelle bleue"
                },
                new CategoryInfo
                {
                    Name = "Autre",
                    Description = "Déchets organiques et autres",
                    RoboflowClasses = new[] { "organic-waste", "phone-cases", "wood-waste", "yoga-mats", "trash" },
                    RecyclingInfo = "Vérifier les instructions locales"
                }
            };

            return Ok(categories);
        }

        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult HealthCheck()
        {
            var health = new
            {
                status = "healthy",
                service = "Roboflow Waste Detection 2.0",
                apiConfigured = !string.IsNullOrEmpty(_roboflowSettings.ApiKey),
                cacheEnabled = true,
                compressionEnabled = true,
                timestamp = DateTime.UtcNow,
                configuration = new
                {
                    maxImageSizeMB = MAX_IMAGE_SIZE_MB,
                    maxBatchSize = MAX_BATCH_SIZE,
                    cacheExpirationHours = CACHE_DURATION_HOURS,
                    compressionThresholdKB = 500
                }
            };

            return Ok(health);
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

                if (string.IsNullOrEmpty(_roboflowSettings.ApiKey))
                {
                    throw new InvalidOperationException("Roboflow API Key is not configured");
                }

                if (string.IsNullOrEmpty(_roboflowSettings.ModelId))
                {
                    throw new InvalidOperationException("Roboflow Model ID is not configured");
                }

                string confidenceParam = _roboflowSettings.ConfidenceThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string url = $"{_roboflowSettings.ApiEndpoint}?api_key={_roboflowSettings.ApiKey}&confidence={confidenceParam}";

                _logger.LogInformation("Calling Roboflow API: {Url}", url.Replace(_roboflowSettings.ApiKey, "***"));

                string base64Image = Convert.ToBase64String(imageBytes);
                using var content = new StringContent(base64Image, Encoding.UTF8, "text/plain");

                var response = await client.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Erreur API Roboflow: {StatusCode} - {Error}",
                        response.StatusCode, responseContent);

                    if (response.StatusCode >= System.Net.HttpStatusCode.InternalServerError && retryCount < maxRetries)
                    {
                        await Task.Delay(1000 * (retryCount + 1));
                        return await CallRoboflowAPI(imageBytes, contentType, retryCount + 1);
                    }

                    throw new HttpRequestException($"Erreur API Roboflow: {response.StatusCode}");
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
                return new WasteClassificationResponse
                {
                    Category = "Autre",
                    Confidence = 0.0,
                    Success = true,
                    Message = "Aucun objet détecté dans l'image"
                };
            }

            var bestPrediction = roboflowResponse.Predictions
                .OrderByDescending(p => p.Confidence)
                .First();

            var categoryMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
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
    }

    // Nouvelle classe de requête pour la classification
    public class ClassifyWasteRequest
    {
        public Guid PointDechetId { get; set; }
    }
}