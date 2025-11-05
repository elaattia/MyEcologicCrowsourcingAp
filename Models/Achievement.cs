using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyEcologicCrowsourcingApp.Models
{
    public class Achievement
    {
        public Guid Id { get; set; }
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int PointsRequired { get; set; }
        public int? ChallengesRequired { get; set; }
        public ChallengeType? SpecificType { get; set; }
        public string? Criteria { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
    }
}
