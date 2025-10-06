using Microsoft.AspNetCore.Mvc;
using MyEcologicCrowsourcingApp.DTOs;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MyEcologicCrowsourcingApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var client = _httpClientFactory.CreateClient();

            var loginDto = new LoginDto
            {
                Email = email,
                Password = password
            };

            var content = new StringContent(JsonSerializer.Serialize(loginDto), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("http://localhost:5008/api/users/login", content); 
            
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Email ou mot de passe incorrect.";
                return View();
            }

            var json = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

            // CORRECTION: HttpOnly = false pour permettre l'accès JavaScript
            Response.Cookies.Append("JwtToken", loginResponse.Token, new CookieOptions
            {
                HttpOnly = false,  // ⚠️ CHANGÉ de true à false
                Secure = false,     // Mettre true en production avec HTTPS
                SameSite = SameSiteMode.Lax,  // ⚠️ CHANGÉ de Strict à Lax
                Expires = DateTimeOffset.UtcNow.AddDays(30)  // ⚠️ Augmenté à 30 jours
            });

            return RedirectToAction("Welcome", "Upload");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("JwtToken");
            return RedirectToAction("Login", "Account");
        }
    }
}