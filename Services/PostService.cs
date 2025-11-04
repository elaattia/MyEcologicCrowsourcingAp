using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.DTOs.Forum;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepo;
        private readonly IForumCategoryRepository _categoryRepo;
        private readonly IPostReactionRepository _reactionRepo;

        public PostService(
            IPostRepository postRepo,
            IForumCategoryRepository categoryRepo,
            IPostReactionRepository reactionRepo)
        {
            _postRepo = postRepo;
            _categoryRepo = categoryRepo;
            _reactionRepo = reactionRepo;
        }

        public async Task<PaginatedResult<PostSummaryDto>> GetPostsAsync(
            PostQueryParameters parameters, 
            Guid? currentUserId = null)
        {
            var (posts, totalCount) = await _postRepo.GetAllAsync(parameters);
            
            var postDtos = posts.Select(p => MapToSummaryDto(p)).ToList();

            return new PaginatedResult<PostSummaryDto>
            {
                Items = postDtos,
                TotalCount = totalCount,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize
            };
        }

        public async Task<PostDto?> GetPostByIdAsync(Guid id, Guid? currentUserId = null)
        {
            var post = await _postRepo.GetByIdAsync(id, includeComments: true);
            if (post == null) return null;

            await _postRepo.IncrementViewCountAsync(id);

            var dto = await MapToDetailDto(post, currentUserId);
            return dto;
        }

        public async Task<IEnumerable<PostSummaryDto>> GetPostsByCategoryAsync(
            Guid categoryId, 
            int pageNumber = 1, 
            int pageSize = 20)
        {
            var posts = await _postRepo.GetByCategoryAsync(categoryId, pageNumber, pageSize);
            return posts.Select(MapToSummaryDto);
        }

        public async Task<IEnumerable<PostSummaryDto>> GetPostsByUserAsync(
            Guid userId, 
            int pageNumber = 1, 
            int pageSize = 20)
        {
            var posts = await _postRepo.GetByUserAsync(userId, pageNumber, pageSize);
            return posts.Select(MapToSummaryDto);
        }

        public async Task<IEnumerable<PostSummaryDto>> GetPinnedPostsAsync(Guid? categoryId = null)
        {
            var posts = await _postRepo.GetPinnedPostsAsync(categoryId);
            return posts.Select(MapToSummaryDto);
        }

        public async Task<PostDto> CreatePostAsync(CreatePostDto dto, Guid userId)
        {
            if (!await _categoryRepo.ExistsAsync(dto.CategoryId))
                throw new KeyNotFoundException("Category not found");

            var post = new Post
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Content = dto.Content,
                ImageUrl = dto.ImageUrl,
                VideoUrl = dto.VideoUrl,
                Tags = dto.Tags.Any() ? string.Join(",", dto.Tags) : null,
                UserId = userId,
                CategoryId = dto.CategoryId,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            };

            await _postRepo.CreateAsync(post);
            
            var createdPost = await _postRepo.GetByIdAsync(post.Id);
            return await MapToDetailDto(createdPost!, userId);
        }

        public async Task<PostDto> UpdatePostAsync(Guid id, UpdatePostDto dto, Guid userId)
        {
            var post = await _postRepo.GetByIdAsync(id);
            if (post == null)
                throw new KeyNotFoundException("Post not found");

            if (post.UserId != userId)
                throw new UnauthorizedAccessException("You can only edit your own posts");

            post.Title = dto.Title;
            post.Content = dto.Content;
            post.ImageUrl = dto.ImageUrl;
            post.VideoUrl = dto.VideoUrl;
            post.Tags = dto.Tags.Any() ? string.Join(",", dto.Tags) : null;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepo.UpdateAsync(post);
            
            var updatedPost = await _postRepo.GetByIdAsync(id);
            return await MapToDetailDto(updatedPost!, userId);
        }

        public async Task<bool> DeletePostAsync(Guid id, Guid userId)
        {
            var post = await _postRepo.GetByIdAsync(id);
            if (post == null) return false;

            if (post.UserId != userId)
                throw new UnauthorizedAccessException("You can only delete your own posts");

            return await _postRepo.DeleteAsync(id);
        }

        public async Task<bool> PinPostAsync(Guid id, Guid adminUserId)
        {
            var post = await _postRepo.GetByIdAsync(id);
            if (post == null) return false;

            post.IsPinned = true;
            await _postRepo.UpdateAsync(post);
            return true;
        }

        public async Task<bool> UnpinPostAsync(Guid id, Guid adminUserId)
        {
            var post = await _postRepo.GetByIdAsync(id);
            if (post == null) return false;

            post.IsPinned = false;
            await _postRepo.UpdateAsync(post);
            return true;
        }

        public async Task<bool> LockPostAsync(Guid id, Guid adminUserId)
        {
            var post = await _postRepo.GetByIdAsync(id);
            if (post == null) return false;

            post.IsLocked = true;
            await _postRepo.UpdateAsync(post);
            return true;
        }

        public async Task<bool> UnlockPostAsync(Guid id, Guid adminUserId)
        {
            var post = await _postRepo.GetByIdAsync(id);
            if (post == null) return false;

            post.IsLocked = false;
            await _postRepo.UpdateAsync(post);
            return true;
        }

        private PostSummaryDto MapToSummaryDto(Post post)
        {
            var contentPreview = post.Content.Length > 200 
                ? post.Content.Substring(0, 200) + "..." 
                : post.Content;

            return new PostSummaryDto
            {
                Id = post.Id,
                Title = post.Title,
                ContentPreview = contentPreview,
                ImageUrl = post.ImageUrl,
                Tags = string.IsNullOrEmpty(post.Tags) 
                    ? new List<string>() 
                    : post.Tags.Split(',').ToList(),
                IsPinned = post.IsPinned,
                IsLocked = post.IsLocked,
                ViewCount = post.ViewCount,
                CommentCount = post.Comments?.Count ?? 0,
                ReactionCount = post.Reactions?.Count ?? 0,
                CreatedAt = post.CreatedAt,
                LastActivityAt = post.LastActivityAt,
                UserId = post.UserId,
                Username = post.User?.Username ?? "Unknown",
                CategoryId = post.CategoryId,
                CategoryName = post.Category?.Name ?? "Unknown"
            };
        }

        private async Task<PostDto> MapToDetailDto(Post post, Guid? currentUserId)
        {
            PostReaction? userReaction = null;
            if (currentUserId.HasValue)
            {
                userReaction = await _reactionRepo.GetByUserAndPostAsync(currentUserId.Value, post.Id);
            }

            return new PostDto
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                VideoUrl = post.VideoUrl,
                Tags = string.IsNullOrEmpty(post.Tags) 
                    ? new List<string>() 
                    : post.Tags.Split(',').ToList(),
                Status = post.Status,
                IsPinned = post.IsPinned,
                IsLocked = post.IsLocked,
                ViewCount = post.ViewCount,
                CommentCount = post.Comments?.Count ?? 0,
                ReactionCount = post.Reactions?.Count ?? 0,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                LastActivityAt = post.LastActivityAt,
                UserId = post.UserId,
                Username = post.User?.Username ?? "Unknown",
                UserEmail = post.User?.Email ?? "",
                CategoryId = post.CategoryId,
                CategoryName = post.Category?.Name ?? "Unknown",
                CategorySlug = post.Category?.Slug ?? "",
                HasUserReacted = userReaction != null,
                UserReactionType = userReaction?.Type
            };
        }
    }
}
