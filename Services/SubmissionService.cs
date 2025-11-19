using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Services.Interfaces;
using System.Text.Json;


namespace MyEcologicCrowsourcingApp.Services
{
    public class SubmissionService : ISubmissionService
    {
        private readonly IChallengeSubmissionRepository _submissionRepo;
        private readonly ISubmissionVoteRepository _voteRepo;
        private readonly IChallengeRepository _challengeRepo;
        private readonly IUserChallengeRepository _userChallengeRepo;
        private readonly IUserStatsRepository _statsRepo;
        private readonly IGeminiAIService _geminiService;

        public SubmissionService(
            IChallengeSubmissionRepository submissionRepo,
            ISubmissionVoteRepository voteRepo,
            IChallengeRepository challengeRepo,
            IUserChallengeRepository userChallengeRepo,
            IUserStatsRepository statsRepo,
            IGeminiAIService geminiService)
        {
            _submissionRepo = submissionRepo;
            _voteRepo = voteRepo;
            _challengeRepo = challengeRepo;
            _userChallengeRepo = userChallengeRepo;
            _statsRepo = statsRepo;
            _geminiService = geminiService;
        }

        public async Task<(IEnumerable<SubmissionDto> Submissions, int TotalCount)> GetAllSubmissionsAsync(SubmissionQueryParameters parameters)
        {
            var (submissions, totalCount) = await _submissionRepo.GetAllAsync(parameters);
            var dtos = new List<SubmissionDto>();

            foreach (var submission in submissions)
            {
                var dto = await MapToDtoAsync(submission);
                dtos.Add(dto);
            }

            return (dtos, totalCount);
        }

        public async Task<SubmissionDto?> GetSubmissionByIdAsync(Guid id)
        {
            var submission = await _submissionRepo.GetByIdAsync(id);
            if (submission == null) return null;

            return await MapToDtoAsync(submission);
        }

        public async Task<IEnumerable<SubmissionDto>> GetUserSubmissionsAsync(Guid userId, int pageNumber = 1, int pageSize = 20)
        {
            var submissions = await _submissionRepo.GetByUserAsync(userId, pageNumber, pageSize);
            var dtos = new List<SubmissionDto>();

            foreach (var submission in submissions)
            {
                var dto = await MapToDtoAsync(submission);
                dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<IEnumerable<SubmissionDto>> GetChallengeSubmissionsAsync(Guid challengeId, int pageNumber = 1, int pageSize = 20)
        {
            var submissions = await _submissionRepo.GetByChallengeAsync(challengeId, pageNumber, pageSize);
            var dtos = new List<SubmissionDto>();

            foreach (var submission in submissions)
            {
                var dto = await MapToDtoAsync(submission);
                dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<SubmissionDto> CreateSubmissionAsync(CreateSubmissionDto dto, Guid userId)
        {
            // Check if user has joined the challenge
            var userChallenge = await _userChallengeRepo.GetByUserAndChallengeAsync(userId, dto.ChallengeId);
            if (userChallenge == null)
                throw new InvalidOperationException("You must join the challenge before submitting");

            // Check if user has already submitted for this challenge (if limited)
            var challenge = await _challengeRepo.GetByIdAsync(dto.ChallengeId);
            if (challenge?.MaxSubmissionsPerUser.HasValue == true)
            {
                var submissionCount = await _submissionRepo.GetSubmissionCountAsync(userId, dto.ChallengeId);
                if (submissionCount >= challenge.MaxSubmissionsPerUser.Value)
                    throw new InvalidOperationException($"Maximum submissions ({challenge.MaxSubmissionsPerUser}) reached for this challenge");
            }

            var submission = new ChallengeSubmission
            {
                Id = Guid.NewGuid(),
                ChallengeId = dto.ChallengeId,
                UserId = userId,
                ProofType = dto.ProofType,
                ProofUrl = dto.ProofUrl,
                Description = dto.Description,
                Location = dto.Location,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Status = SubmissionStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            var created = await _submissionRepo.CreateAsync(submission);

            // Start AI verification in background
            _ = Task.Run(async () => await ProcessSubmissionVerificationAsync(created.Id));

            return await MapToDtoAsync(created);
        }

        public async Task<bool> DeleteSubmissionAsync(Guid id, Guid userId)
        {
            var submission = await _submissionRepo.GetByIdAsync(id);
            if (submission == null) return false;

            // Only allow deletion if user owns the submission or is admin
            if (submission.UserId != userId)
                return false;

            return await _submissionRepo.DeleteAsync(id);
        }

        public async Task<SubmissionDto> ProcessSubmissionVerificationAsync(Guid submissionId)
        {
            var submission = await _submissionRepo.GetByIdAsync(submissionId);
            if (submission == null)
                throw new KeyNotFoundException("Submission not found");

            if (submission.Status != SubmissionStatus.Pending)
                return await MapToDtoAsync(submission);

            try
            {
                var aiResponse = await _geminiService.VerifySubmissionAsync(submission.ProofUrl, submission.ProofType);

                AIResultDto aiResult;
                try
                {
                    aiResult = JsonSerializer.Deserialize<AIResultDto>(aiResponse);
                }
                catch
                {
                    aiResult = new AIResultDto
                    {
                        status = "INVALID",
                        confidence = 0,
                        explanation = "AI parsing failed"
                    };
                }

                submission.IsAIVerified = true;
                submission.AIConfidenceScore = aiResult?.confidence / 100.0; 
                submission.AIVerificationResult = aiResult?.status + " - " + aiResult?.explanation;
                submission.AIVerifiedAt = DateTime.UtcNow;

                // Auto-approve if high confidence
                if (aiResult.confidence >= 0.8)
                {
                    submission.Status = SubmissionStatus.Approved;
                    submission.PointsAwarded = await CalculatePointsAsync(submission.ChallengeId, aiResult.confidence);
                    
                    // Update user stats
                    await UpdateUserStatsAsync(submission.UserId, submission.PointsAwarded);
                    
                    // Mark challenge as completed if applicable
                    await UpdateUserChallengeProgressAsync(submission.UserId, submission.ChallengeId);
                }
                else if (aiResult.confidence >= 0.5)
                {
                    submission.Status = SubmissionStatus.UnderReview;
                }
                else
                {
                    submission.Status = SubmissionStatus.RequiresMoreInfo;
                }

                var updated = await _submissionRepo.UpdateAsync(submission);
                return await MapToDtoAsync(updated);
            }
            catch (Exception ex)
            {
                // If AI verification fails, mark for manual review
                submission.Status = SubmissionStatus.UnderReview;
                submission.AIVerificationResult = $"AI verification failed: {ex.Message}";
                var updated = await _submissionRepo.UpdateAsync(submission);
                return await MapToDtoAsync(updated);
            }
        }

        public async Task<SubmissionDto> ReviewSubmissionAsync(Guid submissionId, ReviewSubmissionDto dto, Guid reviewerId)
        {
            var submission = await _submissionRepo.GetByIdAsync(submissionId);
            if (submission == null)
                throw new KeyNotFoundException("Submission not found");

            submission.Status = dto.Status;
            submission.ReviewedByUserId = reviewerId;
            submission.ReviewNotes = dto.ReviewNotes;
            submission.ReviewedAt = DateTime.UtcNow;

            if (dto.Status == SubmissionStatus.Approved)
            {
                submission.PointsAwarded = dto.PointsAwarded > 0 ? dto.PointsAwarded : await CalculatePointsAsync(submission.ChallengeId);
                
                // Update user stats
                await UpdateUserStatsAsync(submission.UserId, submission.PointsAwarded);
                
                // Mark challenge as completed if applicable
                await UpdateUserChallengeProgressAsync(submission.UserId, submission.ChallengeId);
            }

            var updated = await _submissionRepo.UpdateAsync(submission);
            return await MapToDtoAsync(updated);
        }

        public async Task<IEnumerable<SubmissionDto>> GetPendingSubmissionsAsync(int pageNumber = 1, int pageSize = 50)
        {
            var submissions = await _submissionRepo.GetPendingSubmissionsAsync(pageNumber, pageSize);
            var dtos = new List<SubmissionDto>();

            foreach (var submission in submissions)
            {
                var dto = await MapToDtoAsync(submission);
                dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<bool> VoteOnSubmissionAsync(Guid submissionId, Guid userId, CreateVoteDto dto)
        {
            // Check if user has already voted
            var existingVote = await _voteRepo.GetByUserAndSubmissionAsync(userId, submissionId);
            if (existingVote != null)
            {
                // Update existing vote
                existingVote.IsValid = dto.IsValid;
                existingVote.Comment = dto.Comment;
                existingVote.VotedAt = DateTime.UtcNow;
                await _voteRepo.UpdateAsync(existingVote);
                return true;
            }

            var vote = new SubmissionVote
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                UserId = userId,
                IsValid = dto.IsValid,
                Comment = dto.Comment,
                VotedAt = DateTime.UtcNow
            };

            await _voteRepo.CreateAsync(vote);
            return true;
        }

        public async Task<(int ValidVotes, int InvalidVotes)> GetSubmissionVotesAsync(Guid submissionId)
        {
            return await _voteRepo.GetVoteCountsAsync(submissionId);
        }

        private async Task<int> CalculatePointsAsync(Guid challengeId, double? confidenceScore = null)
        {
            var challenge = await _challengeRepo.GetByIdAsync(challengeId);
            if (challenge == null) return 0;

            var basePoints = challenge.Points;
            
            // Apply confidence multiplier
            if (confidenceScore.HasValue)
            {
                basePoints = (int)(basePoints * confidenceScore.Value);
            }

            return Math.Max(basePoints, 1); // Ensure at least 1 point
        }

        private async Task UpdateUserStatsAsync(Guid userId, int pointsAwarded)
        {
            var stats = await _statsRepo.GetByUserIdAsync(userId);
            if (stats == null)
            {
                stats = new UserStats
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TotalPoints = pointsAwarded,
                    ChallengesCompleted = 1,
                    LastUpdated = DateTime.UtcNow
                };
                await _statsRepo.CreateAsync(stats);
            }
            else
            {
                stats.TotalPoints += pointsAwarded;
                stats.ChallengesCompleted++;
                await _statsRepo.UpdateAsync(stats);
            }
        }

        private async Task UpdateUserChallengeProgressAsync(Guid userId, Guid challengeId)
        {
            var userChallenge = await _userChallengeRepo.GetByUserAndChallengeAsync(userId, challengeId);
            if (userChallenge != null && !userChallenge.IsCompleted)
            {
                userChallenge.IsCompleted = true;
                userChallenge.CompletedAt = DateTime.UtcNow;
                await _userChallengeRepo.UpdateAsync(userChallenge);
            }
        }

        private async Task<SubmissionDto> MapToDtoAsync(ChallengeSubmission submission)
        {
            var (validVotes, invalidVotes) = await _voteRepo.GetVoteCountsAsync(submission.Id);

            return new SubmissionDto
            {
                Id = submission.Id,
                ChallengeId = submission.ChallengeId,
                ChallengeTitle = submission.Challenge?.Title ?? "Unknown Challenge",
                UserId = submission.UserId,
                Username = submission.User?.Username?? "Unknown User",
                ProofType = submission.ProofType,
                ProofUrl = submission.ProofUrl,
                ThumbnailUrl = submission.ThumbnailUrl,
                Description = submission.Description,
                Location = submission.Location,
                Status = submission.Status,
                PointsAwarded = submission.PointsAwarded,
                IsAIVerified = submission.IsAIVerified,
                AIConfidenceScore = submission.AIConfidenceScore,
                AIVerificationResult = submission.AIVerificationResult,
                ReviewNotes = submission.ReviewNotes,
                SubmittedAt = submission.SubmittedAt,
                ReviewedAt = submission.ReviewedAt,
                ValidVotes = validVotes,
                InvalidVotes = invalidVotes
            };
        }
    }
}