using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyEcologicCrowsourcingApp.Models
{
    public class SubmissionVote
    {
        public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public Guid UserId { get; set; }
        public bool IsValid { get; set; }
        [MaxLength(500)]
        public string? Comment { get; set; }
        public DateTime VotedAt { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public ChallengeSubmission Submission { get; set; } = null!;
        [JsonIgnore]
        public User User { get; set; } = null!;
    }
}
