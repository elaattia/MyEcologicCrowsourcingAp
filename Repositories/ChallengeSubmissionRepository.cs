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
    public class ChallengeSubmissionRepository : IChallengeSubmissionRepository
    {
        private readonly EcologicDbContext _context;

        public ChallengeSubmissionRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<ChallengeSubmission> Submissions, int TotalCount)> GetAllAsync(SubmissionQueryParameters parameters)
        {
            var query = _context.ChallengeSubmissions
                .Include(s => s.User)
                .Include(s => s.Challenge)
                .Include(s => s.Votes)
                .AsQueryable();

            if (parameters.ChallengeId.HasValue)
                query = query.Where(s => s.ChallengeId == parameters.ChallengeId.Value);

            if (parameters.UserId.HasValue)
                query = query.Where(s => s.UserId == parameters.UserId.Value);

            if (parameters.Status.HasValue)
                query = query.Where(s => s.Status == parameters.Status.Value);

            if (parameters.IsAIVerified.HasValue)
                query = query.Where(s => s.IsAIVerified == parameters.IsAIVerified.Value);

            if (parameters.FromDate.HasValue)
                query = query.Where(s => s.SubmittedAt >= parameters.FromDate.Value);

            if (parameters.ToDate.HasValue)
                query = query.Where(s => s.SubmittedAt <= parameters.ToDate.Value);

            var totalCount = await query.CountAsync();

            query = parameters.SortBy.ToLower() switch
            {
                "points" => query.OrderByDescending(s => s.PointsAwarded),
                "confidence" => query.OrderByDescending(s => s.AIConfidenceScore),
                _ => query.OrderByDescending(s => s.SubmittedAt)
            };

            var submissions = await query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToListAsync();

            return (submissions, totalCount);
        }

        public async Task<ChallengeSubmission?> GetByIdAsync(Guid id)
        {
            return await _context.ChallengeSubmissions
                .Include(s => s.User)
                .Include(s => s.Challenge)
                .Include(s => s.ReviewedBy)
                .Include(s => s.Votes).ThenInclude(v => v.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<ChallengeSubmission>> GetByUserAsync(Guid userId, int page = 1, int size = 20)
        {
            return await _context.ChallengeSubmissions
                .Include(s => s.Challenge)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SubmittedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChallengeSubmission>> GetByChallengeAsync(Guid challengeId, int page = 1, int size = 20)
        {
            return await _context.ChallengeSubmissions
                .Include(s => s.User)
                .Include(s => s.Votes)
                .Where(s => s.ChallengeId == challengeId)
                .OrderByDescending(s => s.SubmittedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChallengeSubmission>> GetPendingSubmissionsAsync(int page = 1, int size = 50)
        {
            return await _context.ChallengeSubmissions
                .Include(s => s.User)
                .Include(s => s.Challenge)
                .Where(s => s.Status == SubmissionStatus.Pending || s.Status == SubmissionStatus.UnderReview)
                .OrderBy(s => s.SubmittedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
        }

        public async Task<int> GetSubmissionCountAsync(Guid userId, Guid challengeId)
        {
            return await _context.ChallengeSubmissions
                .CountAsync(s => s.UserId == userId && s.ChallengeId == challengeId);
        }

        public async Task<ChallengeSubmission> CreateAsync(ChallengeSubmission submission)
        {
            _context.ChallengeSubmissions.Add(submission);
            await _context.SaveChangesAsync();
            return submission;
        }

        public async Task<ChallengeSubmission> UpdateAsync(ChallengeSubmission submission)
        {
            _context.ChallengeSubmissions.Update(submission);
            await _context.SaveChangesAsync();
            return submission;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var s = await _context.ChallengeSubmissions.FindAsync(id);
            if (s == null) return false;

            _context.ChallengeSubmissions.Remove(s);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetApprovedCountAsync(Guid userId)
        {
            return await _context.ChallengeSubmissions
                .CountAsync(s => s.UserId == userId && s.Status == SubmissionStatus.Approved);
        }

        public async Task<int> GetRejectedCountAsync(Guid userId)
        {
            return await _context.ChallengeSubmissions
                .CountAsync(s => s.UserId == userId && s.Status == SubmissionStatus.Rejected);
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.ChallengeSubmissions
                .CountAsync(s => s.Status == SubmissionStatus.Pending ||
                                s.Status == SubmissionStatus.UnderReview);
        }
    }
}
