using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IChallengeRepository
    {
        Task<(IEnumerable<Challenge> Challenges, int TotalCount)> GetAllAsync(ChallengeQueryParameters parameters);
        Task<Challenge?> GetByIdAsync(Guid id);
        Task<IEnumerable<Challenge>> GetActiveChallenggesAsync();
        Task<IEnumerable<Challenge>> GetFeaturedChallengesAsync();
        Task<IEnumerable<Challenge>> GetByTypeAsync(ChallengeType type, int limit = 10);
        Task<IEnumerable<Challenge>> GetAIGeneratedChallengesAsync(int limit = 10);
        Task<Challenge> CreateAsync(Challenge challenge);
        Task<Challenge> UpdateAsync(Challenge challenge);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> IncrementParticipantsAsync(Guid id);
        Task<bool> DecrementParticipantsAsync(Guid id);
        Task<int> GetParticipantCountAsync(Guid id);
    }
}