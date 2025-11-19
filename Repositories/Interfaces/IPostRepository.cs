using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Forum;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IPostRepository
    {
        Task<(IEnumerable<Post> Posts, int TotalCount)> GetAllAsync(PostQueryParameters parameters);
        Task<Post?> GetByIdAsync(Guid id, bool includeComments = false);
        Task<IEnumerable<Post>> GetByCategoryAsync(Guid categoryId, int pageNumber = 1, int pageSize = 20);
        Task<IEnumerable<Post>> GetByUserAsync(Guid userId, int pageNumber = 1, int pageSize = 20);
        Task<IEnumerable<Post>> GetPinnedPostsAsync(Guid? categoryId = null);
        Task<IEnumerable<Post>> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 20);
        Task<Post> CreateAsync(Post post);
        Task<Post> UpdateAsync(Post post);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> IncrementViewCountAsync(Guid id);
        Task<bool> UpdateLastActivityAsync(Guid id, DateTime lastActivityAt);
        Task<int> GetCommentCountAsync(Guid postId);
        Task<int> GetReactionCountAsync(Guid postId);
    }
}