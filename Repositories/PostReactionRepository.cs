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
    public class PostReactionRepository : IPostReactionRepository
    {
        private readonly EcologicDbContext _context;

        public PostReactionRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<PostReaction?> GetByUserAndPostAsync(Guid userId, Guid postId)
        {
            return await _context.PostReactions
                .FirstOrDefaultAsync(r => r.UserId == userId && r.PostId == postId);
        }

        public async Task<IEnumerable<PostReaction>> GetByPostAsync(Guid postId)
        {
            return await _context.PostReactions
                .Include(r => r.User)
                .Where(r => r.PostId == postId)
                .ToListAsync();
        }

        public async Task<Dictionary<ReactionType, int>> GetReactionSummaryAsync(Guid postId)
        {
            return await _context.PostReactions
                .Where(r => r.PostId == postId)
                .GroupBy(r => r.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);
        }

        public async Task<PostReaction> AddAsync(PostReaction reaction)
        {
            _context.PostReactions.Add(reaction);
            await _context.SaveChangesAsync();
            return reaction;
        }

        public async Task<bool> RemoveAsync(Guid userId, Guid postId)
        {
            var reaction = await GetByUserAndPostAsync(userId, postId);
            if (reaction == null) return false;

            _context.PostReactions.Remove(reaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(PostReaction reaction)
        {
            _context.PostReactions.Update(reaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid postId)
        {
            return await _context.PostReactions
                .AnyAsync(r => r.UserId == userId && r.PostId == postId);
        }
    }
}