using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEcologicCrowsourcingApp.DTOs.Challenge;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChallengesController : ControllerBase
    {
        private readonly IChallengeService _challengeService;

        public ChallengesController(IChallengeService challengeService)
        {
            _challengeService = challengeService;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAllChallenges([FromQuery] ChallengeQueryParameters parameters)
        {
            try
            {
                var userId = GetCurrentUserId();
                var (challenges, totalCount) = await _challengeService.GetAllChallengesAsync(parameters, userId);

                return Ok(new
                {
                    data = challenges,
                    totalCount,
                    pageNumber = parameters.PageNumber,
                    pageSize = parameters.PageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving challenges", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ChallengeDto>> GetChallengeById(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var challenge = await _challengeService.GetChallengeByIdAsync(id, userId);

                if (challenge == null)
                    return NotFound(new { message = "Challenge not found" });

                return Ok(challenge);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving challenge", error = ex.Message });
            }
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<ChallengeDto>>> GetActiveChallenges()
        {
            try
            {
                var userId = GetCurrentUserId();
                var challenges = await _challengeService.GetActiveChallengesAsync(userId);
                return Ok(challenges);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving active challenges", error = ex.Message });
            }
        }

        [HttpGet("featured")]
        public async Task<ActionResult<IEnumerable<ChallengeDto>>> GetFeaturedChallenges()
        {
            try
            {
                var userId = GetCurrentUserId();
                var challenges = await _challengeService.GetFeaturedChallengesAsync(userId);
                return Ok(challenges);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving featured challenges", error = ex.Message });
            }
        }

        [HttpGet("type/{type}")]
        public async Task<ActionResult<IEnumerable<ChallengeDto>>> GetChallengesByType(
            ChallengeType type, 
            [FromQuery] int limit = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                var challenges = await _challengeService.GetChallengesByTypeAsync(type, limit, userId);
                return Ok(challenges);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving challenges by type", error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Representant")]
        public async Task<ActionResult<ChallengeDto>> CreateChallenge([FromBody] CreateChallengeDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var challenge = await _challengeService.CreateChallengeAsync(dto, userId.Value);
                return CreatedAtAction(nameof(GetChallengeById), new { id = challenge.Id }, challenge);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating challenge", error = ex.Message });
            }
        }

        [HttpPost("generate")]
        [Authorize(Roles = "Admin,Representant")]
        public async Task<ActionResult<ChallengeDto>> GenerateAIChallenge([FromBody] GenerateChallengeRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var challenge = await _challengeService.GenerateAIChallengeAsync(request, userId.Value);
                return CreatedAtAction(nameof(GetChallengeById), new { id = challenge.Id }, challenge);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating AI challenge", error = ex.Message });
            }
        }

        [HttpPost("generate/batch")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<ChallengeDto>>> GenerateMultipleAIChallenges(
            [FromBody] GenerateChallengeRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                if (request.Count < 1 || request.Count > 10)
                    return BadRequest(new { message = "Count must be between 1 and 10" });

                var challenges = await _challengeService.GenerateMultipleAIChallengesAsync(request, userId.Value);
                return Ok(challenges);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating multiple challenges", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Representant")]
        public async Task<ActionResult<ChallengeDto>> UpdateChallenge(Guid id, [FromBody] CreateChallengeDto dto)
        {
            try
            {
                var challenge = await _challengeService.UpdateChallengeAsync(id, dto);
                return Ok(challenge);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Challenge not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating challenge", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteChallenge(Guid id)
        {
            try
            {
                var result = await _challengeService.DeleteChallengeAsync(id);
                if (!result)
                    return NotFound(new { message = "Challenge not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting challenge", error = ex.Message });
            }
        }

        [HttpPost("{id}/join")]
        [Authorize]
        public async Task<ActionResult> JoinChallenge(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var result = await _challengeService.JoinChallengeAsync(id, userId.Value);
                
                if (!result)
                    return BadRequest(new { message = "Unable to join challenge" });

                return Ok(new { message = "Successfully joined challenge" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error joining challenge", error = ex.Message });
            }
        }

        [HttpPost("{id}/leave")]
        [Authorize]
        public async Task<ActionResult> LeaveChallenge(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var result = await _challengeService.LeaveChallengeAsync(id, userId.Value);
                
                if (!result)
                    return BadRequest(new { message = "Unable to leave challenge" });

                return Ok(new { message = "Successfully left challenge" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error leaving challenge", error = ex.Message });
            }
        }

        [HttpGet("my/active")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ChallengeDto>>> GetMyActiveChallenges()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var challenges = await _challengeService.GetUserActiveChallengesAsync(userId.Value);
                return Ok(challenges);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user challenges", error = ex.Message });
            }
        }

        [HttpGet("my/completed")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ChallengeDto>>> GetMyCompletedChallenges()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var challenges = await _challengeService.GetUserCompletedChallengesAsync(userId.Value);
                return Ok(challenges);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving completed challenges", error = ex.Message });
            }
        }
    }
}