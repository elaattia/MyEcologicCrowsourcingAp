using System;
using MyEcologicCrowsourcingApp.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyEcologicCrowsourcingApp.Models
{
    public class Challenge
    {
        public Guid Id { get; set; }
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public ChallengeType Type { get; set; }
        [Required]
        public ChallengeDifficulty Difficulty { get; set; }
        [Required]
        public ChallengeFrequency Frequency { get; set; }
        public int Points { get; set; }
        public int BonusPoints { get; set; } = 0;
        public string? ImageUrl { get; set; }
        public string? Icon { get; set; }
        public bool IsAIGenerated { get; set; } = false;
        public string? AIPromptUsed { get; set; }
        public DateTime? AIGeneratedAt { get; set; }
        [Required]
        public string RequiredProofType { get; set; } = "Photo";
        public string? VerificationCriteria { get; set; }
        public string? Tips { get; set; }
        public string? Tags { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public int DurationDays { get; set; } = 7;
        public int? MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; } = 0;
        public int? MaxSubmissionsPerUser { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public VerificationMethod VerificationMethod { get; set; } = VerificationMethod.Hybrid;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedByUserId { get; set; }
        [JsonIgnore]
        public User? CreatedBy { get; set; }
        [JsonIgnore]
        public ICollection<ChallengeSubmission> Submissions { get; set; } = new List<ChallengeSubmission>();
        [JsonIgnore]
        public ICollection<UserChallenge> UserChallenges { get; set; } = new List<UserChallenge>();
    }
}
