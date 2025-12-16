using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEcologicCrowsourcingApp.DTOs.Forum;
using MyEcologicCrowsourcingApp.Services.Interfaces;
/*
namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/forum/reports")]
    [Authorize]
    public class PostReportController : ControllerBase
    {
        private readonly IPostReportService _reportService;

        public PostReportController(IPostReportService reportService)
        {
            _reportService = reportService;
        }

        private Guid GetCurrentUserId()
        {
            return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReportDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var report = await _reportService.CreateReportAsync(dto, userId);
                return CreatedAtAction(nameof(GetById), new { id = report.Id }, report);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Representant")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null)
                return NotFound(new { message = "Report not found" });

            return Ok(report);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Representant")]
        public async Task<IActionResult> GetAll(
            [FromQuery] ReportStatus? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var reports = await _reportService.GetAllReportsAsync(status, pageNumber, pageSize);
            return Ok(reports);
        }

        [HttpPut("{id}/review")]
        [Authorize(Roles = "Admin,Representant")]
        public async Task<IActionResult> Review(Guid id, [FromBody] ReviewReportDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var report = await _reportService.ReviewReportAsync(id, dto, userId);
                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}*/