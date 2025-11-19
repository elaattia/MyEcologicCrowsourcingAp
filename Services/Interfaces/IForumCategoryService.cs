
using MyEcologicCrowsourcingApp.DTOs.Forum;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IForumCategoryService
    {
        Task<IEnumerable<ForumCategoryDto>> GetAllCategoriesAsync(bool includeInactive = false);
        Task<ForumCategoryDto?> GetCategoryByIdAsync(Guid id);
        Task<ForumCategoryDto?> GetCategoryBySlugAsync(string slug);
        Task<ForumCategoryDto> CreateCategoryAsync(CreateForumCategoryDto dto);
        Task<ForumCategoryDto> UpdateCategoryAsync(Guid id, UpdateForumCategoryDto dto);
        Task<bool> DeleteCategoryAsync(Guid id);
    }
}