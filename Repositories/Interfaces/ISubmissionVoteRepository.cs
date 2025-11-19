using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface ISubmissionVoteRepository
    {
        Task<SubmissionVote?> GetByUserAndSubmissionAsync(Guid userId, Guid submissionId);
        Task<IEnumerable<SubmissionVote>> GetBySubmissionAsync(Guid submissionId);
        Task<(int ValidVotes, int InvalidVotes)> GetVoteCountsAsync(Guid submissionId);
        Task<SubmissionVote> CreateAsync(SubmissionVote vote);
        Task<SubmissionVote> UpdateAsync(SubmissionVote vote);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> HasUserVotedAsync(Guid userId, Guid submissionId);
    }
}