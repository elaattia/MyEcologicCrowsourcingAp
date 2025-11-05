using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IChallengeSubmissionRepository
    {
        Task<(IEnumerable<ChallengeSubmission> Submissions, int TotalCount)> GetAllAsync(SubmissionQueryParameters parameters);
        Task<ChallengeSubmission?> GetByIdAsync(Guid id);
        Task<IEnumerable<ChallengeSubmission>> GetByUserAsync(Guid userId, int pageNumber = 1, int pageSize = 20);
        Task<IEnumerable<ChallengeSubmission>> GetByChallengeAsync(Guid challengeId, int pageNumber = 1, int pageSize = 20);
        Task<IEnumerable<ChallengeSubmission>> GetPendingSubmissionsAsync(int pageNumber = 1, int pageSize = 50);
        Task<int> GetSubmissionCountAsync(Guid userId, Guid challengeId);
        Task<ChallengeSubmission> CreateAsync(ChallengeSubmission submission);
        Task<ChallengeSubmission> UpdateAsync(ChallengeSubmission submission);
        Task<bool> DeleteAsync(Guid id);
        Task<int> GetApprovedCountAsync(Guid userId);
        Task<int> GetRejectedCountAsync(Guid userId);
        Task<int> GetPendingCountAsync();
    }
}