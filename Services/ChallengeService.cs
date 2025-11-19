using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Services
{
    public class ChallengeService : IChallengeService
    {
        private readonly IChallengeRepository _challengeRepo;
        private readonly IUserChallengeRepository _userChallengeRepo;
        private readonly IGeminiAIService _geminiService;
        private readonly IUserStatsRepository _statsRepo;

        public ChallengeService(
            IChallengeRepository challengeRepo,
            IUserChallengeRepository userChallengeRepo,
            IGeminiAIService geminiService,
            IUserStatsRepository statsRepo)
        {
            _challengeRepo = challengeRepo;
            _userChallengeRepo = userChallengeRepo;
            _geminiService = geminiService;
            _statsRepo = statsRepo;
        }

        public async Task<(IEnumerable<ChallengeDto> Challenges, int TotalCount)> GetAllChallengesAsync(
            ChallengeQueryParameters parameters, Guid? currentUserId = null)
        {
            var (challenges, totalCount) = await _challengeRepo.GetAllAsync(parameters);
            var dtos = new List<ChallengeDto>();

            foreach (var challenge in challenges)
            {
                var dto = MapToDto(challenge);
                
                if (currentUserId.HasValue)
                {
                    var userChallenge = await _userChallengeRepo.GetByUserAndChallengeAsync(
                        currentUserId.Value, challenge.Id);
                    
                    if (userChallenge != null)
                    {
                        dto.IsUserJoined = true;
                        dto.IsUserCompleted = userChallenge.IsCompleted;
                        dto.UserSubmissionCount = userChallenge.SubmissionCount;
                    }
                }
                
                dtos.Add(dto);
            }

            return (dtos, totalCount);
        }

        public async Task<ChallengeDto?> GetChallengeByIdAsync(Guid id, Guid? currentUserId = null)
        {
            var challenge = await _challengeRepo.GetByIdAsync(id);
            if (challenge == null) return null;

            var dto = MapToDto(challenge);

            if (currentUserId.HasValue)
            {
                var userChallenge = await _userChallengeRepo.GetByUserAndChallengeAsync(
                    currentUserId.Value, id);
                
                if (userChallenge != null)
                {
                    dto.IsUserJoined = true;
                    dto.IsUserCompleted = userChallenge.IsCompleted;
                    dto.UserSubmissionCount = userChallenge.SubmissionCount;
                }
            }

            return dto;
        }

        public async Task<IEnumerable<ChallengeDto>> GetActiveChallengesAsync(Guid? currentUserId = null)
        {
            var challenges = await _challengeRepo.GetActiveChallenggesAsync();
            return await EnrichWithUserData(challenges, currentUserId);
        }

        public async Task<IEnumerable<ChallengeDto>> GetFeaturedChallengesAsync(Guid? currentUserId = null)
        {
            var challenges = await _challengeRepo.GetFeaturedChallengesAsync();
            return await EnrichWithUserData(challenges, currentUserId);
        }

        public async Task<IEnumerable<ChallengeDto>> GetChallengesByTypeAsync(
            ChallengeType type, int limit = 10, Guid? currentUserId = null)
        {
            var challenges = await _challengeRepo.GetByTypeAsync(type, limit);
            return await EnrichWithUserData(challenges, currentUserId);
        }

        public async Task<ChallengeDto> CreateChallengeAsync(CreateChallengeDto dto, Guid creatorUserId)
        {
            var challenge = new Challenge
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                Type = dto.Type,
                Difficulty = dto.Difficulty,
                Frequency = dto.Frequency,
                Points = dto.Points,
                BonusPoints = dto.BonusPoints,
                ImageUrl = dto.ImageUrl,
                Icon = dto.Icon,
                RequiredProofType = dto.RequiredProofType,
                VerificationCriteria = dto.VerificationCriteria,
                Tips = dto.Tips,
                Tags = dto.Tags != null ? string.Join(",", dto.Tags) : null,
                StartDate = dto.StartDate ?? DateTime.UtcNow,
                DurationDays = dto.DurationDays,
                MaxParticipants = dto.MaxParticipants,
                MaxSubmissionsPerUser = dto.MaxSubmissionsPerUser,
                VerificationMethod = dto.VerificationMethod,
                IsActive = true,
                CreatedByUserId = creatorUserId,
                CreatedAt = DateTime.UtcNow
            };

            if (challenge.DurationDays > 0)
            {
                challenge.EndDate = challenge.StartDate.AddDays(challenge.DurationDays);
            }

            var created = await _challengeRepo.CreateAsync(challenge);
            return MapToDto(created);
        }

        public async Task<ChallengeDto> GenerateAIChallengeAsync(
            GenerateChallengeRequestDto request, Guid creatorUserId)
        {
            var aiResult = await _geminiService.GenerateChallengeAsync(request);

            var createDto = new CreateChallengeDto
            {
                Title = aiResult.Title,
                Description = aiResult.Description,
                Type = request.Type ?? ChallengeType.Recycling,
                Difficulty = request.Difficulty ?? ChallengeDifficulty.Medium,
                Frequency = request.Frequency ?? ChallengeFrequency.OneTime,
                Points = aiResult.SuggestedPoints,
                Tips = aiResult.Tips,
                Tags = aiResult.Tags,
                VerificationCriteria = aiResult.VerificationCriteria,
                RequiredProofType = "Photo",
                DurationDays = 7,
                VerificationMethod = VerificationMethod.Hybrid
            };

            var challenge = await CreateChallengeAsync(createDto, creatorUserId);
            
            var entity = await _challengeRepo.GetByIdAsync(challenge.Id);
            if (entity != null)
            {
                entity.IsAIGenerated = true;
                entity.AIGeneratedAt = DateTime.UtcNow;
                entity.AIPromptUsed = $"Type: {request.Type}, Difficulty: {request.Difficulty}, Theme: {request.Theme}";
                await _challengeRepo.UpdateAsync(entity);
            }

            return challenge;
        }

        public async Task<List<ChallengeDto>> GenerateMultipleAIChallengesAsync(
            GenerateChallengeRequestDto request, Guid creatorUserId)
        {
            var results = new List<ChallengeDto>();
            
            for (int i = 0; i < request.Count; i++)
            {
                try
                {
                    var challenge = await GenerateAIChallengeAsync(request, creatorUserId);
                    results.Add(challenge);
                    
                    // Rate limiting delay
                    if (i < request.Count - 1)
                        await Task.Delay(1500);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to generate challenge {i + 1}: {ex.Message}");
                }
            }

            return results;
        }

        public async Task<ChallengeDto> UpdateChallengeAsync(Guid id, CreateChallengeDto dto)
        {
            var challenge = await _challengeRepo.GetByIdAsync(id);
            if (challenge == null)
                throw new KeyNotFoundException("Challenge not found");

            challenge.Title = dto.Title;
            challenge.Description = dto.Description;
            challenge.Type = dto.Type;
            challenge.Difficulty = dto.Difficulty;
            challenge.Frequency = dto.Frequency;
            challenge.Points = dto.Points;
            challenge.BonusPoints = dto.BonusPoints;
            challenge.ImageUrl = dto.ImageUrl;
            challenge.Icon = dto.Icon;
            challenge.RequiredProofType = dto.RequiredProofType;
            challenge.VerificationCriteria = dto.VerificationCriteria;
            challenge.Tips = dto.Tips;
            challenge.Tags = dto.Tags != null ? string.Join(",", dto.Tags) : null;
            challenge.DurationDays = dto.DurationDays;
            challenge.MaxParticipants = dto.MaxParticipants;
            challenge.MaxSubmissionsPerUser = dto.MaxSubmissionsPerUser;
            challenge.VerificationMethod = dto.VerificationMethod;

            var updated = await _challengeRepo.UpdateAsync(challenge);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteChallengeAsync(Guid id)
        {
            return await _challengeRepo.DeleteAsync(id);
        }

        public async Task<bool> JoinChallengeAsync(Guid challengeId, Guid userId)
        {
            var challenge = await _challengeRepo.GetByIdAsync(challengeId);
            if (challenge == null || !challenge.IsActive)
                return false;

            if (challenge.MaxParticipants.HasValue && 
                challenge.CurrentParticipants >= challenge.MaxParticipants.Value)
                return false;

            var existing = await _userChallengeRepo.GetByUserAndChallengeAsync(userId, challengeId);
            if (existing != null)
                return false;

            var userChallenge = new UserChallenge
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ChallengeId = challengeId,
                JoinedAt = DateTime.UtcNow
            };

            await _userChallengeRepo.CreateAsync(userChallenge);
            await _challengeRepo.IncrementParticipantsAsync(challengeId);

            // Update user stats
            var stats = await _statsRepo.GetByUserIdAsync(userId);
            if (stats != null)
            {
                stats.ChallengesInProgress++;
                await _statsRepo.UpdateAsync(stats);
            }

            return true;
        }

        public async Task<bool> LeaveChallengeAsync(Guid challengeId, Guid userId)
        {
            var userChallenge = await _userChallengeRepo.GetByUserAndChallengeAsync(userId, challengeId);
            if (userChallenge == null)
                return false;

            if (userChallenge.IsCompleted)
                return false;

            await _userChallengeRepo.DeleteAsync(userChallenge.Id);
            await _challengeRepo.DecrementParticipantsAsync(challengeId);

            var stats = await _statsRepo.GetByUserIdAsync(userId);
            if (stats != null && stats.ChallengesInProgress > 0)
            {
                stats.ChallengesInProgress--;
                await _statsRepo.UpdateAsync(stats);
            }

            return true;
        }

        public async Task<UserChallenge?> GetUserChallengeProgressAsync(Guid userId, Guid challengeId)
        {
            return await _userChallengeRepo.GetByUserAndChallengeAsync(userId, challengeId);
        }

        public async Task<IEnumerable<ChallengeDto>> GetUserActiveChallengesAsync(Guid userId)
        {
            var userChallenges = await _userChallengeRepo.GetByUserAsync(userId, isCompleted: false);
            return userChallenges.Select(uc => MapToDto(uc.Challenge, uc));
        }

        public async Task<IEnumerable<ChallengeDto>> GetUserCompletedChallengesAsync(Guid userId)
        {
            var userChallenges = await _userChallengeRepo.GetByUserAsync(userId, isCompleted: true);
            return userChallenges.Select(uc => MapToDto(uc.Challenge, uc));
        }

        private async Task<IEnumerable<ChallengeDto>> EnrichWithUserData(
            IEnumerable<Challenge> challenges, Guid? userId)
        {
            var dtos = new List<ChallengeDto>();

            foreach (var challenge in challenges)
            {
                var dto = MapToDto(challenge);
                
                if (userId.HasValue)
                {
                    var userChallenge = await _userChallengeRepo.GetByUserAndChallengeAsync(
                        userId.Value, challenge.Id);
                    
                    if (userChallenge != null)
                    {
                        dto.IsUserJoined = true;
                        dto.IsUserCompleted = userChallenge.IsCompleted;
                        dto.UserSubmissionCount = userChallenge.SubmissionCount;
                    }
                }
                
                dtos.Add(dto);
            }

            return dtos;
        }

        private ChallengeDto MapToDto(Challenge challenge, UserChallenge? userChallenge = null)
        {
            return new ChallengeDto
            {
                Id = challenge.Id,
                Title = challenge.Title,
                Description = challenge.Description,
                Type = challenge.Type,
                Difficulty = challenge.Difficulty,
                Frequency = challenge.Frequency,
                Points = challenge.Points,
                BonusPoints = challenge.BonusPoints,
                ImageUrl = challenge.ImageUrl,
                Icon = challenge.Icon,
                IsAIGenerated = challenge.IsAIGenerated,
                RequiredProofType = challenge.RequiredProofType,
                Tips = challenge.Tips,
                Tags = !string.IsNullOrEmpty(challenge.Tags) 
                    ? challenge.Tags.Split(',').ToList() 
                    : new List<string>(),
                StartDate = challenge.StartDate,
                EndDate = challenge.EndDate,
                DurationDays = challenge.DurationDays,
                MaxParticipants = challenge.MaxParticipants,
                CurrentParticipants = challenge.CurrentParticipants,
                IsActive = challenge.IsActive,
                IsFeatured = challenge.IsFeatured,
                IsUserJoined = userChallenge != null,
                IsUserCompleted = userChallenge?.IsCompleted ?? false,
                UserSubmissionCount = userChallenge?.SubmissionCount ?? 0,
                CreatedAt = challenge.CreatedAt
            };
        }
    }
}