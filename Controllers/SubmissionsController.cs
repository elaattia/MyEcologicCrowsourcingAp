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
    public class SubmissionsController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public SubmissionsController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Representant")]
        public async Task<ActionResult<object>> GetAllSubmissions([FromQuery] SubmissionQueryParameters parameters)
        {
            try
            {
                var (submissions, totalCount) = await _submissionService.GetAllSubmissionsAsync(parameters);

                return Ok(new
                {
                    data = submissions,
                    totalCount,
                    pageNumber = parameters.PageNumber,
                    pageSize = parameters.PageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving submissions", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<SubmissionDto>> GetSubmissionById(Guid id)
        {
            try
            {
                var submission = await _submissionService.GetSubmissionByIdAsync(id);
                if (submission == null)
                    return NotFound(new { message = "Submission not found" });

                return Ok(submission);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving submission", error = ex.Message });
            }
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetMySubmissions(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var submissions = await _submissionService.GetUserSubmissionsAsync(userId.Value, pageNumber, pageSize);
                return Ok(submissions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user submissions", error = ex.Message });
            }
        }

        [HttpGet("challenge/{challengeId}")]
        public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetChallengeSubmissions(
            Guid challengeId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var submissions = await _submissionService.GetChallengeSubmissionsAsync(challengeId, pageNumber, pageSize);
                return Ok(submissions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving challenge submissions", error = ex.Message });
            }
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin,Representant")]
        public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetPendingSubmissions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var submissions = await _submissionService.GetPendingSubmissionsAsync(pageNumber, pageSize);
                return Ok(submissions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving pending submissions", error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<SubmissionDto>> CreateSubmission([FromBody] CreateSubmissionDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var submission = await _submissionService.CreateSubmissionAsync(dto, userId.Value);
                return CreatedAtAction(nameof(GetSubmissionById), new { id = submission.Id }, submission);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating submission", error = ex.Message });
            }
        }

        [HttpPost("{id}/review")]
        [Authorize(Roles = "Admin,Representant")]
        public async Task<ActionResult<SubmissionDto>> ReviewSubmission(
            Guid id,
            [FromBody] ReviewSubmissionDto dto)
        {
            try
            {
                var reviewerId = GetCurrentUserId();
                if (!reviewerId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var submission = await _submissionService.ReviewSubmissionAsync(id, dto, reviewerId.Value);
                return Ok(submission);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Submission not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error reviewing submission", error = ex.Message });
            }
        }

        [HttpPost("{id}/vote")]
        [Authorize]
        public async Task<ActionResult> VoteOnSubmission(
            Guid id,
            [FromBody] CreateVoteDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var result = await _submissionService.VoteOnSubmissionAsync(id, userId.Value, dto);
                if (!result)
                    return BadRequest(new { message = "Unable to process vote" });

                return Ok(new { message = "Vote submitted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error submitting vote", error = ex.Message });
            }
        }

        [HttpGet("{id}/votes")]
        [Authorize]
        public async Task<ActionResult<object>> GetSubmissionVotes(Guid id)
        {
            try
            {
                var (validVotes, invalidVotes) = await _submissionService.GetSubmissionVotesAsync(id);
                return Ok(new { validVotes, invalidVotes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving votes", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteSubmission(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "User not authenticated" });

                var result = await _submissionService.DeleteSubmissionAsync(id, userId.Value);
                if (!result)
                    return NotFound(new { message = "Submission not found or you don't have permission to delete it" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting submission", error = ex.Message });
            }
        }
    }
}