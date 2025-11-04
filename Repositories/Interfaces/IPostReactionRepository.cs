using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Forum;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IPostReactionRepository
    {
        Task<PostReaction?> GetByUserAndPostAsync(Guid userId, Guid postId);
        Task<IEnumerable<PostReaction>> GetByPostAsync(Guid postId);
        Task<Dictionary<ReactionType, int>> GetReactionSummaryAsync(Guid postId);
        Task<PostReaction> AddAsync(PostReaction reaction);
        Task<bool> RemoveAsync(Guid userId, Guid postId);
        Task<bool> UpdateAsync(PostReaction reaction);
        Task<bool> ExistsAsync(Guid userId, Guid postId);
    }
}