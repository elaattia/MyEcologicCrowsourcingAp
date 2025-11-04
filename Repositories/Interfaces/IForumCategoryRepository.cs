using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Forum;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IForumCategoryRepository
    {
        Task<IEnumerable<ForumCategory>> GetAllAsync(bool includeInactive = false);
        Task<ForumCategory?> GetByIdAsync(Guid id);
        Task<ForumCategory?> GetBySlugAsync(string slug);
        Task<ForumCategory> CreateAsync(ForumCategory category);
        Task<ForumCategory> UpdateAsync(ForumCategory category);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);
    }
}