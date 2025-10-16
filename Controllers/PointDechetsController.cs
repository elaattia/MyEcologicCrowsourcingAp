//Controllers/PointDechetsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEcologicCrowsourcingApp.Models;
using System.Security.Claims;
using MyEcologicCrowsourcingApp.DTOs;
using MyEcologicCrowsourcingApp.Data;
using System.Globalization;

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

        public PointDechetController(
            EcologicDbContext context,
            IWebHostEnvironment env,
            ILogger<PointDechetController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
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

                return CreatedAtAction(nameof(GetPointDechet), new { id = pointDechet.Id }, new
                {
                    id = pointDechet.Id,
                    url = imageUrl,
                    latitude = pointDechet.Latitude,
                    longitude = pointDechet.Longitude,
                    zone = pointDechet.Zone,
                    pays = pointDechet.Pays,
                    statut = pointDechet.Statut.ToString(),
                    date = pointDechet.Date,
                    message = "Déchet signalé avec succès! Le type et le volume seront analysés automatiquement."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du signalement: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { message = "Erreur serveur", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPointDechet(Guid id)
        {
            var point = await _context.PointDechets.FindAsync(id);
            if (point == null)
                return NotFound();

            return Ok(point);
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
    }
}