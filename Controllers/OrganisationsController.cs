using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiculesController : Controller
    {
        private readonly IVehiculeService _service;

        public VehiculesController(IVehiculeService service)
        {
            _service = service;
        }

        [HttpGet]
        [AllowAnonymous]  
        public async Task<IActionResult> GetAll()
        {
            var vehicules = await _service.GetAllAsync();
            return Ok(vehicules);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]  
        public async Task<IActionResult> GetById(Guid id)
        {
            var vehicule = await _service.GetByIdAsync(id);
            if (vehicule == null) return NotFound();
            return Ok(vehicule);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]  
        public async Task<IActionResult> Create(Vehicule vehicule)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _service.CreateAsync(vehicule);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]  
        public async Task<IActionResult> Update(Guid id, Vehicule vehicule)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _service.UpdateAsync(id, vehicule);
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