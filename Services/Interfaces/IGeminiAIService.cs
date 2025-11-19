using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;

namespace MyEcologicCrowsourcingApp.Services
{
    public interface IGeminiAIService
    {
        Task<AIGeneratedChallengeDto> GenerateChallengeAsync(GenerateChallengeRequestDto request);
        Task<List<AIGeneratedChallengeDto>> GenerateMultipleChallengesAsync(GenerateChallengeRequestDto request);
        Task<string> VerifySubmissionAsync(string imageUrl, string challengeCriteria);
        Task<double> AnalyzeSubmissionConfidenceAsync(string imageUrl, ChallengeType type);
    }
}