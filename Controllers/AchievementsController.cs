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
    public class AchievementsController : ControllerBase
    {
        private readonly IAchievementService _achievementService;

        public AchievementsController(IAchievementService achievementService)
        {
            _achievementService = achievementService;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AchievementDto>>> GetAllAchievements()
        {
            try
            {
                var userId = GetCurrentUserId();
                var achievements = await _achievementService.GetAllAchievementsAsync(userId);
                return Ok(achievements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving achievements", error = ex.Message });
            }
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<AchievementDto>>> GetMyAchievements()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var achievements = await _achievementService.GetUserAchievementsAsync(userId.Value);
                return Ok(achievements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user achievements", error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AchievementDto>> CreateAchievement([FromBody] CreateAchievementDto dto)
        {
            try
            {
                var achievement = await _achievementService.CreateAchievementAsync(dto);
                return CreatedAtAction(nameof(GetAllAchievements), new { id = achievement.Id }, achievement);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating achievement", error = ex.Message });
            }
        }

        [HttpPost("check/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> CheckAchievements(Guid userId)
        {
            try
            {
                var result = await _achievementService.CheckAndUnlockAchievementsAsync(userId);
                return Ok(new 
                { 
                    message = result ? "New achievements unlocked!" : "No new achievements",
                    newAchievementsUnlocked = result 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking achievements", error = ex.Message });
            }
        }
    }
}