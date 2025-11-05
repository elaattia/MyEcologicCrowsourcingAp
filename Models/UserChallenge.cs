using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyEcologicCrowsourcingApp.Models
{
    public class UserChallenge
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ChallengeId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public bool IsCompleted { get; set; } = false;
        public int SubmissionCount { get; set; } = 0;
        public int PointsEarned { get; set; } = 0;
        [JsonIgnore]
        public User User { get; set; } = null!;
        [JsonIgnore]
        public Challenge Challenge { get; set; } = null!;
    }
}
