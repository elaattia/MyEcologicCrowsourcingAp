using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IUserStatsRepository
    {
        Task<UserStats?> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<UserStats>> GetAllAsync(); // Added method
        Task<IEnumerable<UserStats>> GetTopByPointsAsync(int limit = 10);
        Task<IEnumerable<UserStats>> GetWeeklyLeaderboardAsync(int limit = 10);
        Task<IEnumerable<UserStats>> GetMonthlyLeaderboardAsync(int limit = 10);
        Task<UserStats> CreateAsync(UserStats stats);
        Task<UserStats> UpdateAsync(UserStats stats);
        Task<bool> UpdatePointsAsync(Guid userId, int points);
        Task<bool> IncrementChallengesCompletedAsync(Guid userId);
        Task<bool> UpdateStreakAsync(Guid userId, int currentStreak);
        Task<bool> RecalculateRanksAsync();
    }
}