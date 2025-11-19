using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IChallengeService
    {
        Task<(IEnumerable<ChallengeDto> Challenges, int TotalCount)> GetAllChallengesAsync(ChallengeQueryParameters parameters, Guid? currentUserId = null);
        Task<ChallengeDto?> GetChallengeByIdAsync(Guid id, Guid? currentUserId = null);
        Task<IEnumerable<ChallengeDto>> GetActiveChallengesAsync(Guid? currentUserId = null);
        Task<IEnumerable<ChallengeDto>> GetFeaturedChallengesAsync(Guid? currentUserId = null);
        Task<IEnumerable<ChallengeDto>> GetChallengesByTypeAsync(ChallengeType type, int limit = 10, Guid? currentUserId = null);
        Task<ChallengeDto> CreateChallengeAsync(CreateChallengeDto dto, Guid creatorUserId);
        Task<ChallengeDto> UpdateChallengeAsync(Guid id, CreateChallengeDto dto);
        Task<bool> DeleteChallengeAsync(Guid id);

        Task<ChallengeDto> GenerateAIChallengeAsync(GenerateChallengeRequestDto request, Guid creatorUserId);
        Task<List<ChallengeDto>> GenerateMultipleAIChallengesAsync(GenerateChallengeRequestDto request, Guid creatorUserId);

        Task<bool> JoinChallengeAsync(Guid challengeId, Guid userId);
        Task<bool> LeaveChallengeAsync(Guid challengeId, Guid userId);
        Task<UserChallenge?> GetUserChallengeProgressAsync(Guid userId, Guid challengeId);
        Task<IEnumerable<ChallengeDto>> GetUserActiveChallengesAsync(Guid userId);
        Task<IEnumerable<ChallengeDto>> GetUserCompletedChallengesAsync(Guid userId);
    }
}
