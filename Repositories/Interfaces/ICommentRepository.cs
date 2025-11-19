using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Forum;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface ICommentRepository
    {
        Task<IEnumerable<Comment>> GetByPostAsync(Guid postId);
        Task<Comment?> GetByIdAsync(Guid id);
        Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentCommentId);
        Task<IEnumerable<Comment>> GetByUserAsync(Guid userId, int pageNumber = 1, int pageSize = 20);
        Task<Comment> CreateAsync(Comment comment);
        Task<Comment> UpdateAsync(Comment comment);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<int> GetReactionCountAsync(Guid commentId);
        Task<int> GetReplyCountAsync(Guid commentId);
    }
}