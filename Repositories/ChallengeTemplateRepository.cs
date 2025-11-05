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
    public class ChallengeTemplateRepository : IChallengeTemplateRepository
    {
        private readonly EcologicDbContext _context;

        public ChallengeTemplateRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ChallengeTemplate>> GetAllAsync()
        {
            return await _context.ChallengeTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.Type)
                .ToListAsync();
        }

        public async Task<ChallengeTemplate?> GetByIdAsync(Guid id)
        {
            return await _context.ChallengeTemplates.FindAsync(id);
        }

        public async Task<IEnumerable<ChallengeTemplate>> GetByTypeAsync(ChallengeType type)
        {
            return await _context.ChallengeTemplates
                .Where(t => t.Type == type && t.IsActive)
                .ToListAsync();
        }

        public async Task<ChallengeTemplate?> GetRandomByTypeAsync(ChallengeType type)
        {
            var templates = await GetByTypeAsync(type);
            var list = templates.ToList();
            
            if (!list.Any()) return null;

            var random = new Random();
            var index = random.Next(list.Count);
            return list[index];
        }

        public async Task<ChallengeTemplate> CreateAsync(ChallengeTemplate template)
        {
            _context.ChallengeTemplates.Add(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<ChallengeTemplate> UpdateAsync(ChallengeTemplate template)
        {
            _context.ChallengeTemplates.Update(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var template = await _context.ChallengeTemplates.FindAsync(id);
            if (template == null) return false;

            _context.ChallengeTemplates.Remove(template);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
