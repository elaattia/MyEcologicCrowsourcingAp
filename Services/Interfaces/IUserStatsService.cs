using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IUserStatsService
    {
        Task<UserStatsDto?> GetUserStatsAsync(Guid userId);
        Task<IEnumerable<LeaderboardEntryDto>> GetGlobalLeaderboardAsync(int limit = 100);
        Task<IEnumerable<LeaderboardEntryDto>> GetWeeklyLeaderboardAsync(int limit = 100);
        Task<IEnumerable<LeaderboardEntryDto>> GetMonthlyLeaderboardAsync(int limit = 100);
        Task<bool> UpdateUserPointsAsync(Guid userId, int points);
        Task<bool> RecalculateAllRanksAsync();
        Task<bool> UpdateStreaksAsync();
    }
}
