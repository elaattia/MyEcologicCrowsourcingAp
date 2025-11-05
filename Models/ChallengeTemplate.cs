using System;
using System.ComponentModel.DataAnnotations;

namespace MyEcologicCrowsourcingApp.Models
{
    public class ChallengeTemplate
    {
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public ChallengeType Type { get; set; }
        [Required]
        public string AIPromptTemplate { get; set; } = string.Empty;
        public string? VerificationCriteriaTemplate { get; set; }
        public int MinPoints { get; set; } = 10;
        public int MaxPoints { get; set; } = 100;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
