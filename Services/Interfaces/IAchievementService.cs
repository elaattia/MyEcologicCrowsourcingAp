using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.DTOs.Challenge;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IAchievementService
    {
        Task<IEnumerable<AchievementDto>> GetAllAchievementsAsync(Guid? userId = null);
        Task<IEnumerable<AchievementDto>> GetUserAchievementsAsync(Guid userId);
        Task<AchievementDto> CreateAchievementAsync(CreateAchievementDto dto);
        Task<bool> CheckAndUnlockAchievementsAsync(Guid userId);
    }
}
