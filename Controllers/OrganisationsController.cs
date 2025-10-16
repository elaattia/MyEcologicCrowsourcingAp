using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Services.Interfaces;
using MyEcologicCrowsourcingApp.DTOs;
using System.Security.Claims;

namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganisationsController : Controller
    {
        private readonly IOrganisationService _organisationService;
        private readonly IUserService _userService;

        public OrganisationsController(IOrganisationService organisationService, IUserService userService)
        {
            _organisationService = organisationService;
            _userService = userService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var organisations = await _organisationService.GetAllAsync();
            return Ok(organisations);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var organisation = await _organisationService.GetByIdAsync(id);
            if (organisation == null) return NotFound();
            return Ok(organisation);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] CreateOrganisationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userService.GetByEmailAsync(dto.RepreEmail);
            if (existingUser != null)
                return BadRequest("Un utilisateur avec cet email existe déjà.");

            var organisation = new Organisation
            {
                Nom = dto.Nom,
                NbrVolontaires = dto.NbrVolontaires
            };


            var nouveauRepresentant = new User
            {
                UserId = Guid.NewGuid(),
                Username = dto.RepreUsername,
                Email = dto.RepreEmail,
                Password = dto.ReprePassword,
                Role = UserRole.Representant
            };

            var result = await _organisationService.CreateWithRepresentativeAsync(organisation, nouveauRepresentant);
            if (result.Organisation == null)
                return BadRequest("Erreur lors de la création de l'organisation.");


            var response = new OrganisationCreationResponseDto
            {
                OrganisationId = result.Organisation.OrganisationId,
                Nom = result.Organisation.Nom,
                NbrVolontaires = result.Organisation.NbrVolontaires,
                Token = result.Token
            };

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganisationDto dto)
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

            if (user.Role != UserRole.Admin && user.OrganisationId != id)
                return StatusCode(403, "Vous n'êtes pas autorisé à modifier cette organisation.");

            var organisation = new Organisation
            {
                OrganisationId = id,
                Nom = dto.Nom!,
                NbrVolontaires = dto.NbrVolontaires,
                RepresentantId = user.Role == UserRole.Admin
                    ? (await _organisationService.GetByIdAsync(id))?.RepresentantId ?? Guid.Empty
                    : userId
            };

            var updated = await _organisationService.UpdateAsync(id, organisation);
            if (updated == null) return NotFound();

            var response = new OrganisationResponseDto
            {
                OrganisationId = updated.OrganisationId,
                Nom = updated.Nom,
                NbrVolontaires = updated.NbrVolontaires
            };

            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Token utilisateur invalide.");

            var userId = Guid.Parse(userIdClaim);
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return Unauthorized("Utilisateur non trouvé.");

            if (user.Role != UserRole.Admin && user.OrganisationId != id)
                return StatusCode(403, "Vous n'êtes pas autorisé à supprimer cette organisation.");

            var deleted = await _organisationService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}