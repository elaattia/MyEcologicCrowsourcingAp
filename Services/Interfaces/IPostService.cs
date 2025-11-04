using MyEcologicCrowsourcingApp.DTOs.Forum;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IPostService
    {
        Task<PaginatedResult<PostSummaryDto>> GetPostsAsync(PostQueryParameters parameters, Guid? currentUserId = null);
        Task<PostDto?> GetPostByIdAsync(Guid id, Guid? currentUserId = null);
        Task<IEnumerable<PostSummaryDto>> GetPostsByCategoryAsync(Guid categoryId, int pageNumber = 1, int pageSize = 20);
        Task<IEnumerable<PostSummaryDto>> GetPostsByUserAsync(Guid userId, int pageNumber = 1, int pageSize = 20);
        Task<IEnumerable<PostSummaryDto>> GetPinnedPostsAsync(Guid? categoryId = null);
        Task<PostDto> CreatePostAsync(CreatePostDto dto, Guid userId);
        Task<PostDto> UpdatePostAsync(Guid id, UpdatePostDto dto, Guid userId);
        Task<bool> DeletePostAsync(Guid id, Guid userId);
        Task<bool> PinPostAsync(Guid id, Guid adminUserId);
        Task<bool> UnpinPostAsync(Guid id, Guid adminUserId);
        Task<bool> LockPostAsync(Guid id, Guid adminUserId);
        Task<bool> UnlockPostAsync(Guid id, Guid adminUserId);
    }
}