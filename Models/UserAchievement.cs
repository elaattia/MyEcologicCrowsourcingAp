using System;
using System.Text.Json.Serialization;

namespace MyEcologicCrowsourcingApp.Models
{
    public class UserAchievement
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid AchievementId { get; set; }
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public User User { get; set; } = null!;
        [JsonIgnore]
        public Achievement Achievement { get; set; } = null!;
    }
}
