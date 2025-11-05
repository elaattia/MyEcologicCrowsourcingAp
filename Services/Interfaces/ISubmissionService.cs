using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.DTOs.Challenge;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface ISubmissionService
    {
        Task<(IEnumerable<SubmissionDto> Submissions, int TotalCount)> GetAllSubmissionsAsync(SubmissionQueryParameters parameters);
        Task<SubmissionDto?> GetSubmissionByIdAsync(Guid id);
        Task<IEnumerable<SubmissionDto>> GetUserSubmissionsAsync(Guid userId, int pageNumber = 1, int pageSize = 20);
        Task<IEnumerable<SubmissionDto>> GetChallengeSubmissionsAsync(Guid challengeId, int pageNumber = 1, int pageSize = 20);
        Task<SubmissionDto> CreateSubmissionAsync(CreateSubmissionDto dto, Guid userId);
        Task<bool> DeleteSubmissionAsync(Guid id, Guid userId);

        Task<SubmissionDto> ProcessSubmissionVerificationAsync(Guid submissionId);
        Task<SubmissionDto> ReviewSubmissionAsync(Guid submissionId, ReviewSubmissionDto dto, Guid reviewerId);
        Task<IEnumerable<SubmissionDto>> GetPendingSubmissionsAsync(int pageNumber = 1, int pageSize = 50);

        Task<bool> VoteOnSubmissionAsync(Guid submissionId, Guid userId, CreateVoteDto dto);
        Task<(int ValidVotes, int InvalidVotes)> GetSubmissionVotesAsync(Guid submissionId);
    }
}
