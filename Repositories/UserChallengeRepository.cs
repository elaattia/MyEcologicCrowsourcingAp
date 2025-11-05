using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Data;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;

namespace MyEcologicCrowsourcingApp.Repositories
{
    public class UserChallengeRepository : IUserChallengeRepository
    {
        private readonly EcologicDbContext _context;

        public UserChallengeRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<UserChallenge?> GetByUserAndChallengeAsync(Guid userId, Guid challengeId)
        {
            return await _context.UserChallenges
                .Include(uc => uc.Challenge)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.ChallengeId == challengeId);
        }

        public async Task<IEnumerable<UserChallenge>> GetByUserAsync(Guid userId, bool? isCompleted = null)
        {
            var query = _context.UserChallenges
                .Include(uc => uc.Challenge)
                .Where(uc => uc.UserId == userId);

            if (isCompleted.HasValue)
                query = query.Where(uc => uc.IsCompleted == isCompleted.Value);

            return await query
                .OrderByDescending(uc => uc.JoinedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserChallenge>> GetByChallengeAsync(Guid challengeId)
        {
            return await _context.UserChallenges
                .Include(uc => uc.User)
                .Where(uc => uc.ChallengeId == challengeId)
                .OrderByDescending(uc => uc.PointsEarned)
                .ToListAsync();
        }

        public async Task<UserChallenge> CreateAsync(UserChallenge userChallenge)
        {
            _context.UserChallenges.Add(userChallenge);
            await _context.SaveChangesAsync();
            return userChallenge;
        }

        public async Task<UserChallenge> UpdateAsync(UserChallenge userChallenge)
        {
            _context.UserChallenges.Update(userChallenge);
            await _context.SaveChangesAsync();
            return userChallenge;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var uc = await _context.UserChallenges.FindAsync(id);
            if (uc == null) return false;

            _context.UserChallenges.Remove(uc);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid challengeId)
        {
            return await _context.UserChallenges
                .AnyAsync(uc => uc.UserId == userId && uc.ChallengeId == challengeId);
        }

        public async Task<int> GetCompletedCountAsync(Guid userId)
        {
            return await _context.UserChallenges
                .CountAsync(uc => uc.UserId == userId && uc.IsCompleted);
        }

        public async Task<int> GetInProgressCountAsync(Guid userId)
        {
            return await _context.UserChallenges
                .CountAsync(uc => uc.UserId == userId && !uc.IsCompleted);
        }
    }
}
