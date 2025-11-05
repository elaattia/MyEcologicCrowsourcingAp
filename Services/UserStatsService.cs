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
    public class UserStatsService : IUserStatsService
    {
        private readonly IUserStatsRepository _userStatsRepo;
        private readonly IUserChallengeRepository _userChallengeRepo;
        private readonly IChallengeSubmissionRepository _submissionRepo;
        private readonly IChallengeRepository _challengeRepo;
        private readonly IAchievementService _achievementService;

        public UserStatsService(
            IUserStatsRepository userStatsRepo,
            IUserChallengeRepository userChallengeRepo,
            IChallengeSubmissionRepository submissionRepo,
            IChallengeRepository challengeRepo,
            IAchievementService achievementService)
        {
            _userStatsRepo = userStatsRepo;
            _userChallengeRepo = userChallengeRepo;
            _submissionRepo = submissionRepo;
            _challengeRepo = challengeRepo;
            _achievementService = achievementService;
        }

        public async Task<UserStatsDto?> GetUserStatsAsync(Guid userId)
        {
            var stats = await _userStatsRepo.GetByUserIdAsync(userId);
            if (stats == null)
            {
                // Create initial stats if they don't exist
                stats = new UserStats
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TotalPoints = 0,
                    ChallengesCompleted = 0,
                    ChallengesInProgress = 0,
                    SubmissionsApproved = 0,
                    CurrentStreak = 0,
                    LongestStreak = 0,
                    GlobalRank = 0,
                    WeeklyRank = 0,
                    MonthlyRank = 0,
                    LastUpdated = DateTime.UtcNow
                };
                stats = await _userStatsRepo.CreateAsync(stats);
            }

            // Get additional data
            var challengesByType = await GetChallengesByTypeAsync(userId);
            var recentAchievements = await _achievementService.GetUserAchievementsAsync(userId);

            return new UserStatsDto
            {
                UserId = stats.UserId,
                Username = stats.User?.Username ?? "Unknown User",
                TotalPoints = stats.TotalPoints,
                ChallengesCompleted = stats.ChallengesCompleted,
                ChallengesInProgress = await GetChallengesInProgressCountAsync(userId),
                SubmissionsApproved = await _submissionRepo.GetApprovedCountAsync(userId),
                CurrentStreak = stats.CurrentStreak,
                LongestStreak = stats.LongestStreak,
                GlobalRank = stats.GlobalRank,
                WeeklyRank = stats.WeeklyRank,
                MonthlyRank = stats.MonthlyRank,
                ChallengesByType = challengesByType,
                RecentAchievements = recentAchievements.Take(5).ToList()
            };
        }

        public async Task<IEnumerable<LeaderboardEntryDto>> GetGlobalLeaderboardAsync(int limit = 100)
        {
            var stats = await _userStatsRepo.GetTopByPointsAsync(limit);
            return stats.Select((s, index) => new LeaderboardEntryDto
            {
                Rank = index + 1,
                UserId = s.UserId,
                Username = s.User?.Username ?? "Unknown User",
                TotalPoints = s.TotalPoints,
                ChallengesCompleted = s.ChallengesCompleted,
                CurrentStreak = s.CurrentStreak
            });
        }

        public async Task<IEnumerable<LeaderboardEntryDto>> GetWeeklyLeaderboardAsync(int limit = 100)
        {
            var stats = await _userStatsRepo.GetWeeklyLeaderboardAsync(limit);
            return stats.Select((s, index) => new LeaderboardEntryDto
            {
                Rank = index + 1,
                UserId = s.UserId,
                Username = s.User?.Username ?? "Unknown User",
                TotalPoints = s.TotalPoints,
                ChallengesCompleted = s.ChallengesCompleted,
                CurrentStreak = s.CurrentStreak
            });
        }

        public async Task<IEnumerable<LeaderboardEntryDto>> GetMonthlyLeaderboardAsync(int limit = 100)
        {
            var stats = await _userStatsRepo.GetMonthlyLeaderboardAsync(limit);
            return stats.Select((s, index) => new LeaderboardEntryDto
            {
                Rank = index + 1,
                UserId = s.UserId,
                Username = s.User?.Username ?? "Unknown User",
                TotalPoints = s.TotalPoints,
                ChallengesCompleted = s.ChallengesCompleted,
                CurrentStreak = s.CurrentStreak
            });
        }

        public async Task<bool> UpdateUserPointsAsync(Guid userId, int points)
        {
            var result = await _userStatsRepo.UpdatePointsAsync(userId, points);
            if (result)
            {
                await _achievementService.CheckAndUnlockAchievementsAsync(userId);
            }
            return result;
        }

        public async Task<bool> RecalculateAllRanksAsync()
        {
            return await _userStatsRepo.RecalculateRanksAsync();
        }

        public async Task<bool> UpdateStreaksAsync()
        {
            // This would typically be called by a background service
            var allStats = await _userStatsRepo.GetAllAsync(); // You'll need to add this method to the repository
            foreach (var stats in allStats)
            {
                await UpdateUserStreakAsync(stats.UserId);
            }
            return true;
        }

        private async Task<Dictionary<string, int>> GetChallengesByTypeAsync(Guid userId)
        {
            var userChallenges = await _userChallengeRepo.GetByUserAsync(userId, isCompleted: true);
            var challengesByType = new Dictionary<string, int>();

            foreach (var userChallenge in userChallenges)
            {
                var type = userChallenge.Challenge.Type.ToString();
                if (challengesByType.ContainsKey(type))
                {
                    challengesByType[type]++;
                }
                else
                {
                    challengesByType[type] = 1;
                }
            }

            return challengesByType;
        }

        private async Task<int> GetChallengesInProgressCountAsync(Guid userId)
        {
            var userChallenges = await _userChallengeRepo.GetByUserAsync(userId, isCompleted: false);
            return userChallenges.Count();
        }

        private async Task UpdateUserStreakAsync(Guid userId)
        {
            var stats = await _userStatsRepo.GetByUserIdAsync(userId);
            if (stats == null) return;

            var userChallenges = await _userChallengeRepo.GetByUserAsync(userId, isCompleted: true);
            var completedDates = userChallenges
                .Where(uc => uc.CompletedAt.HasValue)
                .Select(uc => uc.CompletedAt.Value.Date)
                .Distinct()
                .OrderBy(date => date)
                .ToList();

            if (!completedDates.Any())
            {
                // No completed challenges, reset streak
                await _userStatsRepo.UpdateStreakAsync(userId, 0);
                return;
            }

            var currentStreak = CalculateCurrentStreak(completedDates);
            await _userStatsRepo.UpdateStreakAsync(userId, currentStreak);
        }

        private int CalculateCurrentStreak(List<DateTime> completedDates)
        {
            if (!completedDates.Any()) return 0;

            var today = DateTime.UtcNow.Date;
            var currentStreak = 0;

            // Start from today and go backwards
            var currentDate = today;
            
            // Check if user completed a challenge today
            if (completedDates.Contains(currentDate))
            {
                currentStreak = 1;
                currentDate = currentDate.AddDays(-1);
            }
            else
            {
                // No challenge completed today, check yesterday
                currentDate = today.AddDays(-1);
                if (completedDates.Contains(currentDate))
                {
                    currentStreak = 1;
                    currentDate = currentDate.AddDays(-1);
                }
                else
                {
                    // No challenge completed today or yesterday, streak is broken
                    return 0;
                }
            }

            // Continue counting backwards for consecutive days
            while (completedDates.Contains(currentDate))
            {
                currentStreak++;
                currentDate = currentDate.AddDays(-1);
            }

            return currentStreak;
        }

    }
}