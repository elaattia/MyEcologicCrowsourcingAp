using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.DTOs.Forum;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Services
{
    public class ReactionService : IReactionService
    {
        private readonly IPostReactionRepository _postReactionRepo;
        private readonly ICommentReactionRepository _commentReactionRepo;

        public ReactionService(
            IPostReactionRepository postReactionRepo,
            ICommentReactionRepository commentReactionRepo)
        {
            _postReactionRepo = postReactionRepo;
            _commentReactionRepo = commentReactionRepo;
        }

        public async Task<bool> AddPostReactionAsync(Guid postId, ReactionType type, Guid userId)
        {
            var existing = await _postReactionRepo.GetByUserAndPostAsync(userId, postId);
            
            if (existing != null)
            {
                if (existing.Type == type)
                    return await _postReactionRepo.RemoveAsync(userId, postId);
                
                existing.Type = type;
                return await _postReactionRepo.UpdateAsync(existing);
            }

            var reaction = new PostReaction
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = userId,
                Type = type,
                CreatedAt = DateTime.UtcNow
            };

            await _postReactionRepo.AddAsync(reaction);
            return true;
        }

        public async Task<bool> RemovePostReactionAsync(Guid postId, Guid userId)
        {
            return await _postReactionRepo.RemoveAsync(userId, postId);
        }

        public async Task<List<ReactionSummaryDto>> GetPostReactionSummaryAsync(Guid postId, Guid? currentUserId = null)
        {
            var summary = await _postReactionRepo.GetReactionSummaryAsync(postId);
            var result = new List<ReactionSummaryDto>();

            PostReaction? userReaction = null;
            if (currentUserId.HasValue)
            {
                userReaction = await _postReactionRepo.GetByUserAndPostAsync(currentUserId.Value, postId);
            }

            foreach (ReactionType type in Enum.GetValues(typeof(ReactionType)))
            {
                result.Add(new ReactionSummaryDto
                {
                    Type = type,
                    Count = summary.ContainsKey(type) ? summary[type] : 0,
                    UserReacted = userReaction?.Type == type
                });
            }

            return result;
        }

        public async Task<bool> AddCommentReactionAsync(Guid commentId, ReactionType type, Guid userId)
        {
            var existing = await _commentReactionRepo.GetByUserAndCommentAsync(userId, commentId);
            
            if (existing != null)
            {
                if (existing.Type == type)
                    return await _commentReactionRepo.RemoveAsync(userId, commentId);
                
                existing.Type = type;
                return await _commentReactionRepo.UpdateAsync(existing);
            }

            var reaction = new CommentReaction
            {
                Id = Guid.NewGuid(),
                CommentId = commentId,
                UserId = userId,
                Type = type,
                CreatedAt = DateTime.UtcNow
            };

            await _commentReactionRepo.AddAsync(reaction);
            return true;
        }

        public async Task<bool> RemoveCommentReactionAsync(Guid commentId, Guid userId)
        {
            return await _commentReactionRepo.RemoveAsync(userId, commentId);
        }

        public async Task<List<ReactionSummaryDto>> GetCommentReactionSummaryAsync(Guid commentId, Guid? currentUserId = null)
        {
            var summary = await _commentReactionRepo.GetReactionSummaryAsync(commentId);
            var result = new List<ReactionSummaryDto>();

            CommentReaction? userReaction = null;
            if (currentUserId.HasValue)
            {
                userReaction = await _commentReactionRepo.GetByUserAndCommentAsync(currentUserId.Value, commentId);
            }

            foreach (ReactionType type in Enum.GetValues(typeof(ReactionType)))
            {
                result.Add(new ReactionSummaryDto
                {
                    Type = type,
                    Count = summary.ContainsKey(type) ? summary[type] : 0,
                    UserReacted = userReaction?.Type == type
                });
            }

            return result;
        }
    }
}
