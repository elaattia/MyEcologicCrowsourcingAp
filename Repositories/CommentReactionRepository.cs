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
    public class CommentReactionRepository : ICommentReactionRepository
    {
        private readonly EcologicDbContext _context;

        public CommentReactionRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<CommentReaction?> GetByUserAndCommentAsync(Guid userId, Guid commentId)
        {
            return await _context.CommentReactions
                .FirstOrDefaultAsync(r => r.UserId == userId && r.CommentId == commentId);
        }

        public async Task<IEnumerable<CommentReaction>> GetByCommentAsync(Guid commentId)
        {
            return await _context.CommentReactions
                .Include(r => r.User)
                .Where(r => r.CommentId == commentId)
                .ToListAsync();
        }

        public async Task<Dictionary<ReactionType, int>> GetReactionSummaryAsync(Guid commentId)
        {
            return await _context.CommentReactions
                .Where(r => r.CommentId == commentId)
                .GroupBy(r => r.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);
        }

        public async Task<CommentReaction> AddAsync(CommentReaction reaction)
        {
            _context.CommentReactions.Add(reaction);
            await _context.SaveChangesAsync();
            return reaction;
        }

        public async Task<bool> RemoveAsync(Guid userId, Guid commentId)
        {
            var reaction = await GetByUserAndCommentAsync(userId, commentId);
            if (reaction == null) return false;

            _context.CommentReactions.Remove(reaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(CommentReaction reaction)
        {
            _context.CommentReactions.Update(reaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid commentId)
        {
            return await _context.CommentReactions
                .AnyAsync(r => r.UserId == userId && r.CommentId == commentId);
        }
    }
}