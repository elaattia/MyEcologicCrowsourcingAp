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
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepo;
        private readonly IPostRepository _postRepo;
        private readonly ICommentReactionRepository _reactionRepo;

        public CommentService(
            ICommentRepository commentRepo,
            IPostRepository postRepo,
            ICommentReactionRepository reactionRepo)
        {
            _commentRepo = commentRepo;
            _postRepo = postRepo;
            _reactionRepo = reactionRepo;
        }

        public async Task<IEnumerable<CommentDto>> GetCommentsByPostAsync(Guid postId, Guid? currentUserId = null)
        {
            var comments = await _commentRepo.GetByPostAsync(postId);
            var dtos = new List<CommentDto>();

            foreach (var comment in comments)
            {
                dtos.Add(await MapToDto(comment, currentUserId));
            }

            return dtos;
        }

        public async Task<CommentDto?> GetCommentByIdAsync(Guid id, Guid? currentUserId = null)
        {
            var comment = await _commentRepo.GetByIdAsync(id);
            return comment == null ? null : await MapToDto(comment, currentUserId);
        }

        public async Task<CommentDto> CreateCommentAsync(CreateCommentDto dto, Guid userId)
        {
            var post = await _postRepo.GetByIdAsync(dto.PostId);
            if (post == null)
                throw new KeyNotFoundException("Post not found");

            if (post.IsLocked)
                throw new InvalidOperationException("This post is locked and cannot accept new comments");

            if (dto.ParentCommentId.HasValue)
            {
                if (!await _commentRepo.ExistsAsync(dto.ParentCommentId.Value))
                    throw new KeyNotFoundException("Parent comment not found");
            }

            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                Content = dto.Content,
                ImageUrl = dto.ImageUrl,
                PostId = dto.PostId,
                UserId = userId,
                ParentCommentId = dto.ParentCommentId,
                CreatedAt = DateTime.UtcNow
            };

            await _commentRepo.CreateAsync(comment);
            await _postRepo.UpdateLastActivityAsync(dto.PostId, DateTime.UtcNow);

            var createdComment = await _commentRepo.GetByIdAsync(comment.Id);
            return await MapToDto(createdComment!, userId);
        }

        public async Task<CommentDto> UpdateCommentAsync(Guid id, UpdateCommentDto dto, Guid userId)
        {
            var comment = await _commentRepo.GetByIdAsync(id);
            if (comment == null)
                throw new KeyNotFoundException("Comment not found");

            if (comment.UserId != userId)
                throw new UnauthorizedAccessException("You can only edit your own comments");

            comment.Content = dto.Content;
            comment.ImageUrl = dto.ImageUrl;
            comment.IsEdited = true;
            comment.UpdatedAt = DateTime.UtcNow;

            await _commentRepo.UpdateAsync(comment);
            
            var updatedComment = await _commentRepo.GetByIdAsync(id);
            return await MapToDto(updatedComment!, userId);
        }

        public async Task<bool> DeleteCommentAsync(Guid id, Guid userId)
        {
            var comment = await _commentRepo.GetByIdAsync(id);
            if (comment == null) return false;

            if (comment.UserId != userId)
                throw new UnauthorizedAccessException("You can only delete your own comments");

            return await _commentRepo.DeleteAsync(id);
        }

        private async Task<CommentDto> MapToDto(Comment comment, Guid? currentUserId)
        {
            CommentReaction? userReaction = null;
            if (currentUserId.HasValue)
            {
                userReaction = await _reactionRepo.GetByUserAndCommentAsync(currentUserId.Value, comment.Id);
            }

            var dto = new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                ImageUrl = comment.ImageUrl,
                IsEdited = comment.IsEdited,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                UserId = comment.UserId,
                Username = comment.User?.Username ?? "Unknown",
                PostId = comment.PostId,
                ParentCommentId = comment.ParentCommentId,
                ReactionCount = comment.Reactions?.Count ?? 0,
                ReplyCount = comment.Replies?.Count ?? 0,
                HasUserReacted = userReaction != null,
                UserReactionType = userReaction?.Type,
                Replies = new List<CommentDto>()
            };

            if (comment.Replies != null && comment.Replies.Any())
            {
                foreach (var reply in comment.Replies)
                {
                    dto.Replies.Add(await MapToDto(reply, currentUserId));
                }
            }

            return dto;
        }
    }
}
