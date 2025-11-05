using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Services
{
    public class AchievementService : IAchievementService
    {
        private readonly IAchievementRepository _achievementRepo;
        private readonly IUserAchievementRepository _userAchievementRepo;
        private readonly IUserStatsRepository _userStatsRepo;
        private readonly IUserChallengeRepository _userChallengeRepo;

        public AchievementService(
            IAchievementRepository achievementRepo,
            IUserAchievementRepository userAchievementRepo,
            IUserStatsRepository userStatsRepo,
            IUserChallengeRepository userChallengeRepo)
        {
            _achievementRepo = achievementRepo;
            _userAchievementRepo = userAchievementRepo;
            _userStatsRepo = userStatsRepo;
            _userChallengeRepo = userChallengeRepo;
        }

        public async Task<IEnumerable<AchievementDto>> GetAllAchievementsAsync(Guid? userId = null)
        {
            var achievements = await _achievementRepo.GetActiveAsync();
            var dtos = new List<AchievementDto>();

            foreach (var achievement in achievements)
            {
                var dto = MapToDto(achievement);
                
                if (userId.HasValue)
                {
                    dto.IsUnlocked = await _userAchievementRepo.HasUnlockedAsync(userId.Value, achievement.Id);
                    dto.UnlockedAt = dto.IsUnlocked ? 
                        (await _userAchievementRepo.GetByUserAndAchievementAsync(userId.Value, achievement.Id))?.UnlockedAt : null;
                }
                
                dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<IEnumerable<AchievementDto>> GetUserAchievementsAsync(Guid userId)
        {
            var userAchievements = await _userAchievementRepo.GetByUserAsync(userId);
            return userAchievements.Select(ua => new AchievementDto
            {
                Id = ua.Achievement.Id,
                Name = ua.Achievement.Name,
                Description = ua.Achievement.Description,
                Icon = ua.Achievement.Icon,
                PointsRequired = ua.Achievement.PointsRequired,
                IsUnlocked = true,
                UnlockedAt = ua.UnlockedAt
            });
        }

        public async Task<AchievementDto> CreateAchievementAsync(CreateAchievementDto dto)
        {
            var achievement = new Achievement
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                Icon = dto.Icon,
                PointsRequired = dto.PointsRequired,
                ChallengesRequired = dto.ChallengesRequired,
                SpecificType = dto.SpecificType,
                Criteria = dto.Criteria,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _achievementRepo.CreateAsync(achievement);
            return MapToDto(created);
        }

        public async Task<bool> CheckAndUnlockAchievementsAsync(Guid userId)
        {
            var unlockedAny = false;
            var userStats = await _userStatsRepo.GetByUserIdAsync(userId);
            if (userStats == null) return false;

            var achievements = await _achievementRepo.GetActiveAsync();
            var userChallenges = await _userChallengeRepo.GetByUserAsync(userId, isCompleted: true);

            foreach (var achievement in achievements)
            {
                if (await _userAchievementRepo.HasUnlockedAsync(userId, achievement.Id))
                    continue;

                if (await CheckAchievementCriteriaAsync(userId, achievement, userStats, userChallenges))
                {
                    await UnlockAchievementAsync(userId, achievement);
                    unlockedAny = true;
                }
            }

            return unlockedAny;
        }

        private async Task<bool> CheckAchievementCriteriaAsync(Guid userId, Achievement achievement, UserStats userStats, IEnumerable<UserChallenge> userChallenges)
        {
            // Check points requirement
            if (userStats.TotalPoints < achievement.PointsRequired)
                return false;

            // Check challenges requirement
            if (achievement.ChallengesRequired.HasValue)
            {
                var completedChallenges = userChallenges.Count();
                
                // If specific type is required, filter by type
                if (achievement.SpecificType.HasValue)
                {
                    completedChallenges = userChallenges.Count(uc => uc.Challenge.Type == achievement.SpecificType.Value);
                }

                if (completedChallenges < achievement.ChallengesRequired.Value)
                    return false;
            }

            // Additional custom criteria could be implemented here
            if (!string.IsNullOrEmpty(achievement.Criteria))
            {
                // Parse and evaluate custom criteria
                // This is a simplified implementation
                return await EvaluateCustomCriteriaAsync(userId, achievement.Criteria);
            }

            return true;
        }

        private async Task<bool> EvaluateCustomCriteriaAsync(Guid userId, string criteria)
        {
            // Implement custom criteria evaluation logic
            // For now, return true for simple implementation
            return await Task.FromResult(true);
        }

        private async Task UnlockAchievementAsync(Guid userId, Achievement achievement)
        {
            var userAchievement = new UserAchievement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AchievementId = achievement.Id,
                UnlockedAt = DateTime.UtcNow
            };

            await _userAchievementRepo.CreateAsync(userAchievement);
        }

        private AchievementDto MapToDto(Achievement achievement)
        {
            return new AchievementDto
            {
                Id = achievement.Id,
                Name = achievement.Name,
                Description = achievement.Description,
                Icon = achievement.Icon,
                PointsRequired = achievement.PointsRequired,
                IsUnlocked = false,
                UnlockedAt = null
            };
        }
    }
}