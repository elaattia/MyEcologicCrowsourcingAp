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
    public class UserStatsRepository : IUserStatsRepository
    {
        private readonly EcologicDbContext _context;

        public UserStatsRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<UserStats?> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserStats
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<IEnumerable<UserStats>> GetAllAsync()
        {
            return await _context.UserStats
                .Include(s => s.User)
                .OrderByDescending(s => s.TotalPoints)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserStats>> GetTopByPointsAsync(int limit = 10)
        {
            return await _context.UserStats
                .Include(s => s.User)
                .OrderByDescending(s => s.TotalPoints)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserStats>> GetWeeklyLeaderboardAsync(int limit = 10)
        {
            return await _context.UserStats
                .Include(s => s.User)
                .OrderBy(s => s.WeeklyRank)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserStats>> GetMonthlyLeaderboardAsync(int limit = 10)
        {
            return await _context.UserStats
                .Include(s => s.User)
                .OrderBy(s => s.MonthlyRank)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<UserStats> CreateAsync(UserStats stats)
        {
            _context.UserStats.Add(stats);
            await _context.SaveChangesAsync();
            return stats;
        }

        public async Task<UserStats> UpdateAsync(UserStats stats)
        {
            stats.LastUpdated = DateTime.UtcNow;
            _context.UserStats.Update(stats);
            await _context.SaveChangesAsync();
            return stats;
        }

        public async Task<bool> UpdatePointsAsync(Guid userId, int points)
        {
            var stats = await GetByUserIdAsync(userId);
            if (stats == null) return false;

            stats.TotalPoints += points;
            stats.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IncrementChallengesCompletedAsync(Guid userId)
        {
            var stats = await GetByUserIdAsync(userId);
            if (stats == null) return false;

            stats.ChallengesCompleted++;
            stats.LastChallengeCompletedAt = DateTime.UtcNow;
            stats.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStreakAsync(Guid userId, int currentStreak)
        {
            var stats = await GetByUserIdAsync(userId);
            if (stats == null) return false;

            stats.CurrentStreak = currentStreak;
            if (currentStreak > stats.LongestStreak)
                stats.LongestStreak = currentStreak;

            stats.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RecalculateRanksAsync()
        {
            var allStats = await _context.UserStats
                .OrderByDescending(s => s.TotalPoints)
                .ToListAsync();

            for (int i = 0; i < allStats.Count; i++)
            {
                allStats[i].GlobalRank = i + 1;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}