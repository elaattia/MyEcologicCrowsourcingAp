using MyEcologicCrowsourcingApp.DTOs.Forum;
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IReactionService
    {
        Task<bool> AddPostReactionAsync(Guid postId, ReactionType type, Guid userId);
        Task<bool> RemovePostReactionAsync(Guid postId, Guid userId);
        Task<List<ReactionSummaryDto>> GetPostReactionSummaryAsync(Guid postId, Guid? currentUserId = null);

        Task<bool> AddCommentReactionAsync(Guid commentId, ReactionType type, Guid userId);
        Task<bool> RemoveCommentReactionAsync(Guid commentId, Guid userId);
        Task<List<ReactionSummaryDto>> GetCommentReactionSummaryAsync(Guid commentId, Guid? currentUserId = null);
    }
}