using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IUserChallengeRepository
    {
        Task<UserChallenge?> GetByUserAndChallengeAsync(Guid userId, Guid challengeId);
        Task<IEnumerable<UserChallenge>> GetByUserAsync(Guid userId, bool? isCompleted = null);
        Task<IEnumerable<UserChallenge>> GetByChallengeAsync(Guid challengeId);
        Task<UserChallenge> CreateAsync(UserChallenge userChallenge);
        Task<UserChallenge> UpdateAsync(UserChallenge userChallenge);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid userId, Guid challengeId);
        Task<int> GetCompletedCountAsync(Guid userId);
        Task<int> GetInProgressCountAsync(Guid userId);
    }
}