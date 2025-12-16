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

        // 🔧 CORRECTION CRITIQUE: Bien sauvegarder le challenge généré par IA
        public async Task<ChallengeDto> GenerateAIChallengeAsync(
            GenerateChallengeRequestDto request, Guid creatorUserId)
        {
            try
            {
                // 1. Générer le contenu avec Gemini
                Console.WriteLine($"🤖 Génération IA - Theme: {request.Theme}, Type: {request.Type}");
                var aiResult = await _geminiService.GenerateChallengeAsync(request);
                Console.WriteLine($"✅ IA a généré: {aiResult.Title}");

                // 2. Créer l'entité Challenge
                var challenge = new Challenge
                {
                    Id = Guid.NewGuid(),
                    Title = aiResult.Title,
                    Description = aiResult.Description,
                    Type = request.Type ?? ChallengeType.Recycling,
                    Difficulty = request.Difficulty ?? ChallengeDifficulty.Medium,
                    Frequency = request.Frequency ?? ChallengeFrequency.OneTime,
                    Points = aiResult.SuggestedPoints,
                    BonusPoints = 0,
                    Tips = aiResult.Tips,
                    Tags = aiResult.Tags != null ? string.Join(",", aiResult.Tags) : null,
                    VerificationCriteria = aiResult.VerificationCriteria,
                    RequiredProofType = "Photo",
                    StartDate = DateTime.UtcNow,
                    DurationDays = 7,
                    VerificationMethod = VerificationMethod.Hybrid,
                    IsActive = true,
                    IsFeatured = false,
                    CurrentParticipants = 0,
                    // ✅ Marquer comme généré par IA
                    IsAIGenerated = true,
                    AIGeneratedAt = DateTime.UtcNow,
                    AIPromptUsed = $"Type: {request.Type}, Difficulty: {request.Difficulty}, Theme: {request.Theme}",
                    CreatedByUserId = creatorUserId,
                    CreatedAt = DateTime.UtcNow
                };

                if (challenge.DurationDays > 0)
                {
                    challenge.EndDate = challenge.StartDate.AddDays(challenge.DurationDays);
                }

                // 3. ✅ SAUVEGARDER EN BASE DE DONNÉES
                Console.WriteLine($"💾 Sauvegarde du challenge: {challenge.Id}");
                var created = await _challengeRepo.CreateAsync(challenge);
                Console.WriteLine($"✅ Challenge sauvegardé avec succès!");

                // 4. Retourner le DTO
                return MapToDto(created);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur génération IA: {ex.Message}");
                throw;
            }
        }

        // 🔧 CORRECTION: Générer plusieurs challenges et tous les sauvegarder
        public async Task<List<ChallengeDto>> GenerateMultipleAIChallengesAsync(
            GenerateChallengeRequestDto request, Guid creatorUserId)
        {
            var results = new List<ChallengeDto>();
            var count = Math.Min(request.Count, 10); // Limiter à 10 max
            
            Console.WriteLine($"🤖 Génération de {count} challenges en lot...");
            
            for (int i = 0; i < count; i++)
            {
                try
                {
                    Console.WriteLine($"📝 Génération challenge {i + 1}/{count}");
                    
                    // Varier les paramètres pour plus de diversité
                    var variedRequest = new GenerateChallengeRequestDto
                    {
                        Type = request.Type,
                        Difficulty = request.Difficulty,
                        Frequency = request.Frequency,
                        Theme = request.Theme,
                        Count = 1
                    };

                    // ✅ Utiliser la méthode qui sauvegarde déjà
                    var challenge = await GenerateAIChallengeAsync(variedRequest, creatorUserId);
                    results.Add(challenge);
                    
                    Console.WriteLine($"✅ Challenge {i + 1}/{count} créé: {challenge.Title}");
                    
                    // Rate limiting pour ne pas surcharger l'API Gemini
                    if (i < count - 1)
                    {
                        Console.WriteLine($"⏳ Attente 1.5s avant le prochain...");
                        await Task.Delay(1500);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Échec challenge {i + 1}: {ex.Message}");
                    // Continuer même si un challenge échoue
                }
            }

            Console.WriteLine($"✅ Génération terminée: {results.Count}/{count} challenges créés");
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