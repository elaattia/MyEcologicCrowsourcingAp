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
    public class PostRepository : IPostRepository
    {
        private readonly EcologicDbContext _context;

        public PostRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Post> Posts, int TotalCount)> GetAllAsync(PostQueryParameters parameters)
        {
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Category)
                .AsQueryable();

            if (parameters.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == parameters.CategoryId.Value);

            if (parameters.Status.HasValue)
                query = query.Where(p => p.Status == parameters.Status.Value);
            else
                query = query.Where(p => p.Status == PostStatus.Published);

            if (parameters.IsPinned.HasValue)
                query = query.Where(p => p.IsPinned == parameters.IsPinned.Value);

            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(searchTerm) ||
                                        p.Content.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(parameters.Tag))
                query = query.Where(p => p.Tags != null && p.Tags.Contains(parameters.Tag));

            var totalCount = await query.CountAsync();

            query = parameters.SortBy.ToLower() switch
            {
                "popular" => query.OrderByDescending(p => p.ViewCount),
                "mostcommented" => query.OrderByDescending(p => p.Comments.Count),
                "oldest" => query.OrderBy(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.IsPinned)
                          .ThenByDescending(p => p.LastActivityAt ?? p.CreatedAt)
            };

            var posts = await query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToListAsync();

            return (posts, totalCount);
        }

        public async Task<Post?> GetByIdAsync(Guid id, bool includeComments = false)
        {
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.Reactions)
                .AsQueryable();

            if (includeComments)
            {
                query = query
                    .Include(p => p.Comments)
                        .ThenInclude(c => c.User)
                    .Include(p => p.Comments)
                        .ThenInclude(c => c.Reactions);
            }

            return await query.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Post>> GetByCategoryAsync(Guid categoryId, int pageNumber = 1, int pageSize = 20)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Where(p => p.CategoryId == categoryId && p.Status == PostStatus.Published)
                .OrderByDescending(p => p.IsPinned)
                .ThenByDescending(p => p.LastActivityAt ?? p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetByUserAsync(Guid userId, int pageNumber = 1, int pageSize = 20)
        {
            return await _context.Posts
                .Include(p => p.Category)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPinnedPostsAsync(Guid? categoryId = null)
        {
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.IsPinned && p.Status == PostStatus.Published);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            return await query
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 20)
        {
            var lowerSearchTerm = searchTerm.ToLower();
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.Status == PostStatus.Published &&
                           (p.Title.ToLower().Contains(lowerSearchTerm) ||
                            p.Content.ToLower().Contains(lowerSearchTerm)))
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Post> CreateAsync(Post post)
        {
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<Post> UpdateAsync(Post post)
        {
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return false;

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Posts.AnyAsync(p => p.Id == id);
        }

        public async Task<bool> IncrementViewCountAsync(Guid id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return false;

            post.ViewCount++;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateLastActivityAsync(Guid id, DateTime lastActivityAt)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return false;

            post.LastActivityAt = lastActivityAt;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetCommentCountAsync(Guid postId)
        {
            return await _context.Comments.CountAsync(c => c.PostId == postId);
        }

        public async Task<int> GetReactionCountAsync(Guid postId)
        {
            return await _context.PostReactions.CountAsync(r => r.PostId == postId);
        }
    }
}