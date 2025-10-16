using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Services.Interfaces;
using System.Security.Claims;

namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class DepotsController : Controller
    {
        private readonly IDepotService _depotService;
        private readonly IUserService _userService;

        public DepotsController(IDepotService depotService, IUserService userService)
        {
            _depotService = depotService;
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Token utilisateur invalide.");

            var userId = Guid.Parse(userIdClaim);
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return Unauthorized("Utilisateur non trouvé.");

            if (user.Role != UserRole.Representant)
                return StatusCode(403, "Seuls les représentants peuvent consulter les dépôts.");

            var depots = await _depotService.GetAllAsync();
            var depotsOrg = depots.Where(d => d.OrganisationId == user.OrganisationId);
            
            return Ok(depotsOrg);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Token utilisateur invalide.");

            var userId = Guid.Parse(userIdClaim);
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return Unauthorized("Utilisateur non trouvé.");

            if (user.Role != UserRole.Representant)
                return StatusCode(403, "Seuls les représentants peuvent consulter les dépôts.");

            var depot = await _depotService.GetByIdAsync(id);
            if (depot == null) return NotFound();

            if (depot.OrganisationId != user.OrganisationId)
                return StatusCode(403, "Vous ne pouvez consulter que les dépôts de votre organisation.");

            return Ok(depot);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Depot depot)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Token utilisateur invalide.");

            var userId = Guid.Parse(userIdClaim);
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return Unauthorized("Utilisateur non trouvé.");

            if (user.Role != UserRole.Representant || user.OrganisationId != depot.OrganisationId)
                return StatusCode(403, "Vous n'êtes pas autorisé à créer un dépôt pour cette organisation.");

            var created = await _depotService.CreateAsync(depot);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Depot depot)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Token utilisateur invalide.");

            var userId = Guid.Parse(userIdClaim);
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return Unauthorized("Utilisateur non trouvé.");

            if (user.Role != UserRole.Representant || user.OrganisationId != depot.OrganisationId)
                return StatusCode(403, "Vous n'êtes pas autorisé à modifier ce dépôt.");

            var updated = await _depotService.UpdateAsync(id, depot);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var depot = await _depotService.GetByIdAsync(id);
            if (depot == null) return NotFound();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Token utilisateur invalide.");

            var userId = Guid.Parse(userIdClaim);
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return Unauthorized("Utilisateur non trouvé.");

            if (user.Role != UserRole.Representant || user.OrganisationId != depot.OrganisationId)
                return StatusCode(403, "Vous n'êtes pas autorisé à supprimer ce dépôt.");

            var deleted = await _depotService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}