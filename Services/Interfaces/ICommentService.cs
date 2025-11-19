using MyEcologicCrowsourcingApp.DTOs.Forum;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface ICommentService
    {
        Task<IEnumerable<CommentDto>> GetCommentsByPostAsync(Guid postId, Guid? currentUserId = null);
        Task<CommentDto?> GetCommentByIdAsync(Guid id, Guid? currentUserId = null);
        Task<CommentDto> CreateCommentAsync(CreateCommentDto dto, Guid userId);
        Task<CommentDto> UpdateCommentAsync(Guid id, UpdateCommentDto dto, Guid userId);
        Task<bool> DeleteCommentAsync(Guid id, Guid userId);
    }
}