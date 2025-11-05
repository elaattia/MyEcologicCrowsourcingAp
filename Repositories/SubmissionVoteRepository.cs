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
    public class SubmissionVoteRepository : ISubmissionVoteRepository
    {
        private readonly EcologicDbContext _context;

        public SubmissionVoteRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<SubmissionVote?> GetByUserAndSubmissionAsync(Guid userId, Guid submissionId)
        {
            return await _context.SubmissionVotes
                .FirstOrDefaultAsync(v => v.UserId == userId && v.SubmissionId == submissionId);
        }

        public async Task<IEnumerable<SubmissionVote>> GetBySubmissionAsync(Guid submissionId)
        {
            return await _context.SubmissionVotes
                .Include(v => v.User)
                .Where(v => v.SubmissionId == submissionId)
                .OrderByDescending(v => v.VotedAt)
                .ToListAsync();
        }

        public async Task<(int ValidVotes, int InvalidVotes)> GetVoteCountsAsync(Guid submissionId)
        {
            var votes = await _context.SubmissionVotes
                .Where(v => v.SubmissionId == submissionId)
                .ToListAsync();

            var validVotes = votes.Count(v => v.IsValid);
            var invalidVotes = votes.Count(v => !v.IsValid);

            return (validVotes, invalidVotes);
        }

        public async Task<SubmissionVote> CreateAsync(SubmissionVote vote)
        {
            _context.SubmissionVotes.Add(vote);
            await _context.SaveChangesAsync();
            return vote;
        }

        public async Task<SubmissionVote> UpdateAsync(SubmissionVote vote)
        {
            _context.SubmissionVotes.Update(vote);
            await _context.SaveChangesAsync();
            return vote;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var vote = await _context.SubmissionVotes.FindAsync(id);
            if (vote == null) return false;

            _context.SubmissionVotes.Remove(vote);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasUserVotedAsync(Guid userId, Guid submissionId)
        {
            return await _context.SubmissionVotes
                .AnyAsync(v => v.UserId == userId && v.SubmissionId == submissionId);
        }
    }
}
