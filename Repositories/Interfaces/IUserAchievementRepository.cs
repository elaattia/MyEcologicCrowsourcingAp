using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IUserAchievementRepository
    {
        Task<IEnumerable<UserAchievement>> GetByUserAsync(Guid userId);
        Task<UserAchievement?> GetByUserAndAchievementAsync(Guid userId, Guid achievementId);
        Task<UserAchievement> CreateAsync(UserAchievement userAchievement);
        Task<bool> HasUnlockedAsync(Guid userId, Guid achievementId);
        Task<int> GetUnlockedCountAsync(Guid userId);
    }
}