using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEcologicCrowsourcingApp.DTOs.Forum;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/forum/reports")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost]
        public async Task<IActionResult> ReportPost([FromBody] CreateReportDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var report = await _reportService.ReportPostAsync(dto, userId);
                return CreatedAtAction(nameof(GetById), new { id = report.Id }, report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] ReportStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var reports = await _reportService.GetAllReportsAsync(status, page, pageSize);
            return Ok(reports);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null)
                return NotFound(new { message = "Report not found" });

            return Ok(report);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/review")]
        public async Task<IActionResult> ReviewReport(Guid id, [FromBody] ReviewReportDto dto)
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var report = await _reportService.ReviewReportAsync(id, dto, adminUserId);
                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("pending/count")]
        public async Task<IActionResult> GetPendingCount()
        {
            var count = await _reportService.GetPendingReportCountAsync();
            return Ok(new { count });
        }
    }
}
