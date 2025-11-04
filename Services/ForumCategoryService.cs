using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.DTOs.Forum;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Services
{
    public class ForumCategoryService : IForumCategoryService
    {
        private readonly IForumCategoryRepository _categoryRepo;

        public ForumCategoryService(IForumCategoryRepository categoryRepo)
        {
            _categoryRepo = categoryRepo;
        }

        public async Task<IEnumerable<ForumCategoryDto>> GetAllCategoriesAsync(bool includeInactive = false)
        {
            var categories = await _categoryRepo.GetAllAsync(includeInactive);
            return categories.Select(MapToDto);
        }

        public async Task<ForumCategoryDto?> GetCategoryByIdAsync(Guid id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            return category == null ? null : MapToDto(category);
        }

        public async Task<ForumCategoryDto?> GetCategoryBySlugAsync(string slug)
        {
            var category = await _categoryRepo.GetBySlugAsync(slug);
            return category == null ? null : MapToDto(category);
        }

        public async Task<ForumCategoryDto> CreateCategoryAsync(CreateForumCategoryDto dto)
        {
            var slug = GenerateSlug(dto.Name);
            
            if (await _categoryRepo.SlugExistsAsync(slug))
                throw new InvalidOperationException("A category with this name already exists");

            var category = new ForumCategory
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                Slug = slug,
                Icon = dto.Icon,
                DisplayOrder = dto.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };

            await _categoryRepo.CreateAsync(category);
            return MapToDto(category);
        }

        public async Task<ForumCategoryDto> UpdateCategoryAsync(Guid id, UpdateForumCategoryDto dto)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null)
                throw new KeyNotFoundException("Category not found");

            var slug = GenerateSlug(dto.Name);
            
            if (await _categoryRepo.SlugExistsAsync(slug, id))
                throw new InvalidOperationException("A category with this name already exists");

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.Slug = slug;
            category.Icon = dto.Icon;
            category.DisplayOrder = dto.DisplayOrder;
            category.IsActive = dto.IsActive;

            await _categoryRepo.UpdateAsync(category);
            return MapToDto(category);
        }

        public async Task<bool> DeleteCategoryAsync(Guid id)
        {
            return await _categoryRepo.DeleteAsync(id);
        }

        private ForumCategoryDto MapToDto(ForumCategory category)
        {
            return new ForumCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Slug = category.Slug,
                Icon = category.Icon,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                PostCount = category.Posts?.Count ?? 0,
                LastActivityAt = category.Posts?.OrderByDescending(p => p.LastActivityAt ?? p.CreatedAt)
                    .FirstOrDefault()?.LastActivityAt
            };
        }

        private string GenerateSlug(string name)
        {
            var slug = name.ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            return slug.Trim('-');
        }
    }
}
