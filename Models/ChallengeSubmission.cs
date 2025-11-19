using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyEcologicCrowsourcingApp.Models
{
    public class ChallengeSubmission
    {
        public Guid Id { get; set; }
        public Guid ChallengeId { get; set; }
        public Guid UserId { get; set; }
        [Required]
        public string ProofType { get; set; } = "Photo";
        [Required]
        public string ProofUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        [MaxLength(1000)]
        public string? Description { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
        public int PointsAwarded { get; set; } = 0;
        public bool IsAIVerified { get; set; } = false;
        public double? AIConfidenceScore { get; set; }
        public string? AIVerificationResult { get; set; }
        public DateTime? AIVerifiedAt { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public string? ReviewNotes { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? Metadata { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public Challenge Challenge { get; set; } = null!;
        [JsonIgnore]
        public User User { get; set; } = null!;
        [JsonIgnore]
        public User? ReviewedBy { get; set; }
        [JsonIgnore]
        public ICollection<SubmissionVote> Votes { get; set; } = new List<SubmissionVote>();
    }
}
