//controllers/usercontroller
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEcologicCrowsourcingApp.DTOs;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;

        public UsersController(IUserService userService, IJwtService jwtService)
        {
            _userService = userService;
            _jwtService = jwtService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();

            var safeUsers = users.Select(u => new
            {
                u.UserId,
                u.Username,
                u.Email,
                u.Role
            });

            return Ok(safeUsers);
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Utilisateur non trouvé." });

            return Ok(new
            {
                user.UserId,
                user.Username,
                user.Email,
                user.Role
            });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            try
            {
                if (user == null ||
                    string.IsNullOrWhiteSpace(user.Email) ||
                    string.IsNullOrWhiteSpace(user.Password) ||
                    string.IsNullOrWhiteSpace(user.Username))
                {
                    return BadRequest(new { message = "Tous les champs sont obligatoires." });
                }

                var existingUser = await _userService.GetByEmailAsync(user.Email);
                if (existingUser != null)
                    return Conflict(new { message = "Cet email est déjà utilisé." });

                await _userService.CreateAsync(user);

                return Ok(new { message = "Inscription réussie." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur interne du serveur.", error = ex.Message });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (loginDto == null ||
                    string.IsNullOrWhiteSpace(loginDto.Email) ||
                    string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    return BadRequest(new { message = "Email et mot de passe requis." });
                }

                var user = await _userService.AuthenticateAsync(loginDto.Email, loginDto.Password);

                if (user == null)
                    return Unauthorized(new { message = "Email ou mot de passe incorrect." });

                var token = _jwtService.GenerateToken(user);

                return Ok(new
                {
                    message = "Connexion réussie.",
                    token,
                    user = new
                    {
                        user.UserId,
                        user.Username,
                        user.Email,
                        user.Role
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur interne du serveur.", error = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, [FromBody] User user)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var updatedUser = await _userService.UpdateAsync(id, user);

                if (updatedUser == null)
                    return NotFound(new { message = "Utilisateur introuvable." });

                return Ok(new
                {
                    message = "Profil mis à jour avec succès.",
                    updatedUser = new
                    {
                        updatedUser.UserId,
                        updatedUser.Username,
                        updatedUser.Email,
                        updatedUser.Role
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur interne du serveur.", error = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var deleted = await _userService.DeleteAsync(id);
                if (!deleted)
                    return NotFound(new { message = "Utilisateur introuvable." });

                return Ok(new { message = "Utilisateur supprimé avec succès." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur interne du serveur.", error = ex.Message });
            }
        }
    }
}
