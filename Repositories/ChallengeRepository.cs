using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Data;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;

namespace MyEcologicCrowsourcingApp.Repositories
{
    public class ChallengeRepository : IChallengeRepository
    {
        private readonly EcologicDbContext _context;

        public ChallengeRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Challenge> Challenges, int TotalCount)> GetAllAsync(ChallengeQueryParameters parameters)
        {
            var query = _context.Challenges.AsQueryable();

            if (parameters.Type.HasValue)
                query = query.Where(c => c.Type == parameters.Type.Value);

            if (parameters.Difficulty.HasValue)
                query = query.Where(c => c.Difficulty == parameters.Difficulty.Value);

            if (parameters.Frequency.HasValue)
                query = query.Where(c => c.Frequency == parameters.Frequency.Value);

            if (parameters.IsActive.HasValue)
                query = query.Where(c => c.IsActive == parameters.IsActive.Value);

            if (parameters.IsFeatured.HasValue)
                query = query.Where(c => c.IsFeatured == parameters.IsFeatured.Value);

            if (parameters.IsAIGenerated.HasValue)
                query = query.Where(c => c.IsAIGenerated == parameters.IsAIGenerated.Value);

            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var s = parameters.SearchTerm.ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(s) || c.Description.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(parameters.Tag))
                query = query.Where(c => c.Tags != null && c.Tags.Contains(parameters.Tag));

            var totalCount = await query.CountAsync();

            query = parameters.SortBy.ToLower() switch
            {
                "popular" => query.OrderByDescending(c => c.CurrentParticipants),
                "points" => query.OrderByDescending(c => c.Points),
                "difficulty" => query.OrderBy(c => c.Difficulty),
                _ => query.OrderByDescending(c => c.IsFeatured).ThenByDescending(c => c.CreatedAt)
            };

            var challenges = await query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToListAsync();

            return (challenges, totalCount);
        }

        public async Task<Challenge?> GetByIdAsync(Guid id)
        {
            return await _context.Challenges
                .Include(c => c.CreatedBy)
                .Include(c => c.Submissions)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Challenge>> GetActiveChallenggesAsync()
        {
            return await _context.Challenges
                .Where(c => c.IsActive && c.StartDate <= DateTime.UtcNow &&
                           (!c.EndDate.HasValue || c.EndDate.Value >= DateTime.UtcNow))
                .OrderByDescending(c => c.IsFeatured)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Challenge>> GetFeaturedChallengesAsync()
        {
            return await _context.Challenges
                .Where(c => c.IsFeatured && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync();
        }

        public async Task<IEnumerable<Challenge>> GetByTypeAsync(ChallengeType type, int limit = 10)
        {
            return await _context.Challenges
                .Where(c => c.Type == type && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Challenge>> GetAIGeneratedChallengesAsync(int limit = 10)
        {
            return await _context.Challenges
                .Where(c => c.IsAIGenerated && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<Challenge> CreateAsync(Challenge challenge)
        {
            _context.Challenges.Add(challenge);
            await _context.SaveChangesAsync();
            return challenge;
        }

        public async Task<Challenge> UpdateAsync(Challenge challenge)
        {
            _context.Challenges.Update(challenge);
            await _context.SaveChangesAsync();
            return challenge;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var challenge = await _context.Challenges.FindAsync(id);
            if (challenge == null) return false;

            _context.Challenges.Remove(challenge);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Challenges.AnyAsync(c => c.Id == id);
        }

        public async Task<bool> IncrementParticipantsAsync(Guid id)
        {
            var challenge = await _context.Challenges.FindAsync(id);
            if (challenge == null) return false;

            challenge.CurrentParticipants++;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DecrementParticipantsAsync(Guid id)
        {
            var challenge = await _context.Challenges.FindAsync(id);
            if (challenge == null) return false;

            if (challenge.CurrentParticipants > 0)
                challenge.CurrentParticipants--;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetParticipantCountAsync(Guid id)
        {
            var challenge = await _context.Challenges.FindAsync(id);
            return challenge?.CurrentParticipants ?? 0;
        }
    }
}
