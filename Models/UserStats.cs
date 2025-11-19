using System;
using System.Text.Json.Serialization;

namespace MyEcologicCrowsourcingApp.Models
{
    public class UserStats
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int TotalPoints { get; set; } = 0;
        public int ChallengesCompleted { get; set; } = 0;
        public int ChallengesInProgress { get; set; } = 0;
        public int SubmissionsApproved { get; set; } = 0;
        public int SubmissionsRejected { get; set; } = 0;
        public int CurrentStreak { get; set; } = 0;
        public int LongestStreak { get; set; } = 0;
        public DateTime? LastChallengeCompletedAt { get; set; }
        public int RecyclingChallenges { get; set; } = 0;
        public int LitterPickupChallenges { get; set; } = 0;
        public int PlantingChallenges { get; set; } = 0;
        public int IdentificationChallenges { get; set; } = 0;
        public int GlobalRank { get; set; } = 0;
        public int WeeklyRank { get; set; } = 0;
        public int MonthlyRank { get; set; } = 0;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public User User { get; set; } = null!;
    }
}
