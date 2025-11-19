using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyEcologicCrowsourcingApp.DTOs.Forum;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Controllers
{
    [ApiController]
    [Route("api/forum/reactions")]
    [Authorize]
    public class ReactionController : ControllerBase
    {
        private readonly IReactionService _reactionService;

        public ReactionController(IReactionService reactionService)
        {
            _reactionService = reactionService;
        }

        private Guid GetCurrentUserId()
        {
            return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        [HttpPost("post/{postId}")]
        public async Task<IActionResult> AddPostReaction(Guid postId, [FromBody] AddReactionDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _reactionService.AddPostReactionAsync(postId, dto.Type, userId);
            return Ok(new { success = result });
        }

        [HttpDelete("post/{postId}")]
        public async Task<IActionResult> RemovePostReaction(Guid postId)
        {
            var userId = GetCurrentUserId();
            var result = await _reactionService.RemovePostReactionAsync(postId, userId);
            return Ok(new { success = result });
        }

        [AllowAnonymous]
        [HttpGet("post/{postId}/summary")]
        public async Task<IActionResult> GetPostReactionSummary(Guid postId)
        {
            var currentUserId = User.Identity?.IsAuthenticated == true 
                ? Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value) 
                : (Guid?)null;
            
            var summary = await _reactionService.GetPostReactionSummaryAsync(postId, currentUserId);
            return Ok(summary);
        }

        [HttpPost("comment/{commentId}")]
        public async Task<IActionResult> AddCommentReaction(Guid commentId, [FromBody] AddReactionDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _reactionService.AddCommentReactionAsync(commentId, dto.Type, userId);
            return Ok(new { success = result });
        }

        [HttpDelete("comment/{commentId}")]
        public async Task<IActionResult> RemoveCommentReaction(Guid commentId)
        {
            var userId = GetCurrentUserId();
            var result = await _reactionService.RemoveCommentReactionAsync(commentId, userId);
            return Ok(new { success = result });
        }

        [AllowAnonymous]
        [HttpGet("comment/{commentId}/summary")]
        public async Task<IActionResult> GetCommentReactionSummary(Guid commentId)
        {
            var currentUserId = User.Identity?.IsAuthenticated == true 
                ? Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value) 
                : (Guid?)null;
            
            var summary = await _reactionService.GetCommentReactionSummaryAsync(commentId, currentUserId);
            return Ok(summary);
        }
    }
}
