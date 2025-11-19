using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Data;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Forum;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;

namespace MyEcologicCrowsourcingApp.Repositories
{
    public class ForumCategoryRepository : IForumCategoryRepository
    {
        private readonly EcologicDbContext _context;

        public ForumCategoryRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ForumCategory>> GetAllAsync(bool includeInactive = false)
        {
            var query = _context.ForumCategories.AsQueryable();

            if (!includeInactive)
                query = query.Where(c => c.IsActive);

            return await query.OrderBy(c => c.DisplayOrder).ToListAsync();
        }

        public async Task<ForumCategory?> GetByIdAsync(Guid id)
        {
            return await _context.ForumCategories.FindAsync(id);
        }

        public async Task<ForumCategory?> GetBySlugAsync(string slug)
        {
            return await _context.ForumCategories
                .FirstOrDefaultAsync(c => c.Slug == slug);
        }

        public async Task<ForumCategory> CreateAsync(ForumCategory category)
        {
            _context.ForumCategories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<ForumCategory> UpdateAsync(ForumCategory category)
        {
            _context.ForumCategories.Update(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var category = await _context.ForumCategories.FindAsync(id);
            if (category == null) return false;

            _context.ForumCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.ForumCategories.AnyAsync(c => c.Id == id);
        }

        public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null)
        {
            var query = _context.ForumCategories.Where(c => c.Slug == slug);
            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);

            return await query.AnyAsync();
        }
    }
}