using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.DTOs.Challenge
{
    public class ChallengeDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ChallengeType Type { get; set; }
        public ChallengeDifficulty Difficulty { get; set; }
        public ChallengeFrequency Frequency { get; set; }
        public int Points { get; set; }
        public int BonusPoints { get; set; }
        public string? ImageUrl { get; set; }
        public string? Icon { get; set; }
        public bool IsAIGenerated { get; set; }
        public string RequiredProofType { get; set; } = string.Empty;
        public string? Tips { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int DurationDays { get; set; }
        public int? MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsUserJoined { get; set; }
        public bool IsUserCompleted { get; set; }
        public int UserSubmissionCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AIResultDto
    {
        public string? status { get; set; }
        public double confidence { get; set; }
        public string? explanation { get; set; }
    }

    public class CreateChallengeDto
    {
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

        [Required]
        [Range(1, 1000)]
        public int Points { get; set; }

        public int BonusPoints { get; set; } = 0;

        public string? ImageUrl { get; set; }

        public string? Icon { get; set; }

        [Required]
        public string RequiredProofType { get; set; } = "Photo";

        public string? VerificationCriteria { get; set; }

        public string? Tips { get; set; }

        public List<string> Tags { get; set; } = new();

        public DateTime? StartDate { get; set; }

        public int DurationDays { get; set; } = 7;

        public int? MaxParticipants { get; set; }

        public int? MaxSubmissionsPerUser { get; set; } = 1;

        public VerificationMethod VerificationMethod { get; set; } = VerificationMethod.Hybrid;
    }

    public class GenerateChallengeRequestDto
    {
        public ChallengeType? Type { get; set; }
        public ChallengeDifficulty? Difficulty { get; set; }
        public ChallengeFrequency? Frequency { get; set; }
        public string? Theme { get; set; }
        public string? UserLocation { get; set; }
        public string? Season { get; set; }
        public int Count { get; set; } = 1;
    }

    public class SubmissionDto
    {
        public Guid Id { get; set; }
        public Guid ChallengeId { get; set; }
        public string ChallengeTitle { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ProofType { get; set; } = string.Empty;
        public string ProofUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public SubmissionStatus Status { get; set; }
        public int PointsAwarded { get; set; }
        public bool IsAIVerified { get; set; }
        public double? AIConfidenceScore { get; set; }
        public string? AIVerificationResult { get; set; }
        public string? ReviewNotes { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int ValidVotes { get; set; }
        public int InvalidVotes { get; set; }
    }

    public class CreateSubmissionDto
    {
        [Required]
        public Guid ChallengeId { get; set; }

        [Required]
        public string ProofType { get; set; } = "Photo";

        [Required]
        [Url]
        public string ProofUrl { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public string? Location { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
    }

    public class ReviewSubmissionDto
    {
        [Required]
        public SubmissionStatus Status { get; set; }

        public int PointsAwarded { get; set; }

        [MaxLength(1000)]
        public string? ReviewNotes { get; set; }
    }

    public class CreateVoteDto
    {
        [Required]
        public bool IsValid { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }
    }

    public class UserStatsDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int ChallengesCompleted { get; set; }
        public int ChallengesInProgress { get; set; }
        public int SubmissionsApproved { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public int GlobalRank { get; set; }
        public int WeeklyRank { get; set; }
        public int MonthlyRank { get; set; }
        public Dictionary<string, int> ChallengesByType { get; set; } = new();
        public List<AchievementDto> RecentAchievements { get; set; } = new();
    }

    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int ChallengesCompleted { get; set; }
        public int CurrentStreak { get; set; }
    }

    public class AchievementDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int PointsRequired { get; set; }
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
    }

    public class CreateAchievementDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Icon { get; set; } = string.Empty;

        public int PointsRequired { get; set; }

        public int? ChallengesRequired { get; set; }

        public ChallengeType? SpecificType { get; set; }

        public string? Criteria { get; set; }
    }

    public class ChallengeQueryParameters
    {
        public ChallengeType? Type { get; set; }
        public ChallengeDifficulty? Difficulty { get; set; }
        public ChallengeFrequency? Frequency { get; set; }
        public bool? IsActive { get; set; } = true;
        public bool? IsFeatured { get; set; }
        public bool? IsAIGenerated { get; set; }
        public string? SearchTerm { get; set; }
        public string? Tag { get; set; }
        public string SortBy { get; set; } = "recent"; // recent, popular, points, difficulty
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class SubmissionQueryParameters
    {
        public Guid? ChallengeId { get; set; }
        public Guid? UserId { get; set; }
        public SubmissionStatus? Status { get; set; }
        public bool? IsAIVerified { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SortBy { get; set; } = "recent";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AIGeneratedChallengeDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Tips { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public string VerificationCriteria { get; set; } = string.Empty;
        public int SuggestedPoints { get; set; }
    }
}