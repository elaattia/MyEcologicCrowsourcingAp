using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEcologicCrowsourcingApp.DTOs.Challenge;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly IUserStatsService _statsService;

        public StatsController(IUserStatsService statsService)
        {
            _statsService = statsService;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserStatsDto>> GetMyStats()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var stats = await _statsService.GetUserStatsAsync(userId.Value);
                if (stats == null)
                    return NotFound(new { message = "User stats not found" });

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user stats", error = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<UserStatsDto>> GetUserStats(Guid userId)
        {
            try
            {
                var stats = await _statsService.GetUserStatsAsync(userId);
                if (stats == null)
                    return NotFound(new { message = "User stats not found" });

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user stats", error = ex.Message });
            }
        }

        [HttpGet("leaderboard/global")]
        public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetGlobalLeaderboard(
            [FromQuery] int limit = 100)
        {
            try
            {
                var leaderboard = await _statsService.GetGlobalLeaderboardAsync(limit);
                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving leaderboard", error = ex.Message });
            }
        }

        [HttpGet("leaderboard/weekly")]
        public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetWeeklyLeaderboard(
            [FromQuery] int limit = 100)
        {
            try
            {
                var leaderboard = await _statsService.GetWeeklyLeaderboardAsync(limit);
                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving weekly leaderboard", error = ex.Message });
            }
        }

        [HttpGet("leaderboard/monthly")]
        public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetMonthlyLeaderboard(
            [FromQuery] int limit = 100)
        {
            try
            {
                var leaderboard = await _statsService.GetMonthlyLeaderboardAsync(limit);
                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving monthly leaderboard", error = ex.Message });
            }
        }

        [HttpPost("recalculate-ranks")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RecalculateRanks()
        {
            try
            {
                var result = await _statsService.RecalculateAllRanksAsync();
                return Ok(new { message = "Ranks recalculated successfully", success = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error recalculating ranks", error = ex.Message });
            }
        }
    }
}