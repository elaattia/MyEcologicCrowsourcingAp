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
    public class CommentRepository : ICommentRepository
    {
        private readonly EcologicDbContext _context;

        public CommentRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Comment>> GetByPostAsync(Guid postId)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Reactions)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .Where(c => c.PostId == postId && c.ParentCommentId == null)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Comment?> GetByIdAsync(Guid id)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Reactions)
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentCommentId)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Reactions)
                .Where(c => c.ParentCommentId == parentCommentId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Comment>> GetByUserAsync(Guid userId, int pageNumber = 1, int pageSize = 20)
        {
            return await _context.Comments
                .Include(c => c.Post)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Comment> CreateAsync(Comment comment)
        {
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<Comment> UpdateAsync(Comment comment)
        {
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return false;

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Comments.AnyAsync(c => c.Id == id);
        }

        public async Task<int> GetReactionCountAsync(Guid commentId)
        {
            return await _context.CommentReactions.CountAsync(r => r.CommentId == commentId);
        }

        public async Task<int> GetReplyCountAsync(Guid commentId)
        {
            return await _context.Comments.CountAsync(c => c.ParentCommentId == commentId);
        }
    }
}