//Controllers/VehiculesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganisationsController : Controller
    {
        private readonly IOrganisationService _service;

        public OrganisationsController(IOrganisationService service)
        {
            _service = service;
        }

        [HttpGet]
        [AllowAnonymous]  
        public async Task<IActionResult> GetAll()
        {
            var organisations = await _service.GetAllAsync();
            return Ok(organisations);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]  
        public async Task<IActionResult> GetById(Guid id)
        {
            var organisation = await _service.GetByIdAsync(id);
            if (organisation == null) return NotFound();
            return Ok(organisation);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Create(Organisation organisation)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _service.CreateAsync(organisation);
            return CreatedAtAction(nameof(GetById), new { id = created.OrganisationId }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]  
        public async Task<IActionResult> Update(Guid id, Organisation organisation)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _service.UpdateAsync(id, organisation);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}