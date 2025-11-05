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
    public class AchievementRepository : IAchievementRepository
    {
        private readonly EcologicDbContext _context;

        public AchievementRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Achievement>> GetAllAsync()
        {
            return await _context.Achievements
                .OrderBy(a => a.PointsRequired)
                .ToListAsync();
        }

        public async Task<Achievement?> GetByIdAsync(Guid id)
        {
            return await _context.Achievements.FindAsync(id);
        }

        public async Task<IEnumerable<Achievement>> GetActiveAsync()
        {
            return await _context.Achievements
                .Where(a => a.IsActive)
                .OrderBy(a => a.PointsRequired)
                .ToListAsync();
        }

        public async Task<Achievement> CreateAsync(Achievement achievement)
        {
            _context.Achievements.Add(achievement);
            await _context.SaveChangesAsync();
            return achievement;
        }

        public async Task<Achievement> UpdateAsync(Achievement achievement)
        {
            _context.Achievements.Update(achievement);
            await _context.SaveChangesAsync();
            return achievement;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var achievement = await _context.Achievements.FindAsync(id);
            if (achievement == null) return false;

            _context.Achievements.Remove(achievement);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
