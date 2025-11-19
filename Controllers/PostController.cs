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
    [Route("api/forum/posts")]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostController(IPostService postService)
        {
            _postService = postService;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PostQueryParameters parameters)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _postService.GetPostsAsync(parameters, currentUserId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            var post = await _postService.GetPostByIdAsync(id, currentUserId);
            
            if (post == null)
                return NotFound(new { message = "Post not found" });

            return Ok(post);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(Guid categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var posts = await _postService.GetPostsByCategoryAsync(categoryId, page, pageSize);
            return Ok(posts);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var posts = await _postService.GetPostsByUserAsync(userId, page, pageSize);
            return Ok(posts);
        }

        [HttpGet("pinned")]
        public async Task<IActionResult> GetPinned([FromQuery] Guid? categoryId = null)
        {
            var posts = await _postService.GetPinnedPostsAsync(categoryId);
            return Ok(posts);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var post = await _postService.CreatePostAsync(dto, userId);
                return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePostDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var post = await _postService.UpdatePostAsync(id, dto, userId);
                return Ok(post);
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
                var result = await _postService.DeletePostAsync(id, userId);
                
                if (!result)
                    return NotFound(new { message = "Post not found" });

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/pin")]
        public async Task<IActionResult> Pin(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _postService.PinPostAsync(id, userId);
            
            if (!result)
                return NotFound(new { message = "Post not found" });

            return Ok(new { message = "Post pinned successfully" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/unpin")]
        public async Task<IActionResult> Unpin(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _postService.UnpinPostAsync(id, userId);
            
            if (!result)
                return NotFound(new { message = "Post not found" });

            return Ok(new { message = "Post unpinned successfully" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/lock")]
        public async Task<IActionResult> Lock(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _postService.LockPostAsync(id, userId);
            
            if (!result)
                return NotFound(new { message = "Post not found" });

            return Ok(new { message = "Post locked successfully" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> Unlock(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _postService.UnlockPostAsync(id, userId);
            
            if (!result)
                return NotFound(new { message = "Post not found" });

            return Ok(new { message = "Post unlocked successfully" });
        }
    }
}
