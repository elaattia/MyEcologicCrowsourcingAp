using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Forum;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface ICommentReactionRepository
    {
        Task<CommentReaction?> GetByUserAndCommentAsync(Guid userId, Guid commentId);
        Task<IEnumerable<CommentReaction>> GetByCommentAsync(Guid commentId);
        Task<Dictionary<ReactionType, int>> GetReactionSummaryAsync(Guid commentId);
        Task<CommentReaction> AddAsync(CommentReaction reaction);
        Task<bool> RemoveAsync(Guid userId, Guid commentId);
        Task<bool> UpdateAsync(CommentReaction reaction);
        Task<bool> ExistsAsync(Guid userId, Guid commentId);
    }
}
