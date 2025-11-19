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
    public class UserAchievementRepository : IUserAchievementRepository
    {
        private readonly EcologicDbContext _context;

        public UserAchievementRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserAchievement>> GetByUserAsync(Guid userId)
        {
            return await _context.UserAchievements
                .Include(ua => ua.Achievement)
                .Where(ua => ua.UserId == userId)
                .OrderByDescending(ua => ua.UnlockedAt)
                .ToListAsync();
        }

        public async Task<UserAchievement?> GetByUserAndAchievementAsync(Guid userId, Guid achievementId)
        {
            return await _context.UserAchievements
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);
        }

        public async Task<UserAchievement> CreateAsync(UserAchievement userAchievement)
        {
            _context.UserAchievements.Add(userAchievement);
            await _context.SaveChangesAsync();
            return userAchievement;
        }

        public async Task<bool> HasUnlockedAsync(Guid userId, Guid achievementId)
        {
            return await _context.UserAchievements
                .AnyAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);
        }

        public async Task<int> GetUnlockedCountAsync(Guid userId)
        {
            return await _context.UserAchievements
                .CountAsync(ua => ua.UserId == userId);
        }
    }
}
