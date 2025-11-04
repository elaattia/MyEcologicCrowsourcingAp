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
    [Route("api/forum/comments")]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
        }

        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetByPost(Guid postId)
        {
            var currentUserId = GetCurrentUserId();
            var comments = await _commentService.GetCommentsByPostAsync(postId, currentUserId);
            return Ok(comments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            var comment = await _commentService.GetCommentByIdAsync(id, currentUserId);
            
            if (comment == null)
                return NotFound(new { message = "Comment not found" });

            return Ok(comment);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCommentDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var comment = await _commentService.CreateCommentAsync(dto, userId);
                return CreatedAtAction(nameof(GetById), new { id = comment.Id }, comment);
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

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCommentDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var comment = await _commentService.UpdateCommentAsync(id, dto, userId);
                return Ok(comment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var result = await _commentService.DeleteCommentAsync(id, userId);
                
                if (!result)
                    return NotFound(new { message = "Comment not found" });

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
