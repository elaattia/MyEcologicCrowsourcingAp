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
    public class GeminiAIService : IGeminiAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

        public GeminiAIService(HttpClient httpClient, IOptions<GeminiSettings> settings)
        {
            _httpClient = httpClient;
            _apiKey = settings.Value.ApiKey;
        }

        public async Task<AIGeneratedChallengeDto> GenerateChallengeAsync(GenerateChallengeRequestDto request)
        {
            var prompt = BuildChallengePrompt(request);
            var response = await CallGeminiAPIAsync(prompt);
            return ParseChallengeResponse(response, request);
        }

        public async Task<List<AIGeneratedChallengeDto>> GenerateMultipleChallengesAsync(GenerateChallengeRequestDto request)
        {
            var challenges = new List<AIGeneratedChallengeDto>();
            
            for (int i = 0; i < request.Count; i++)
            {
                try
                {
                    var challenge = await GenerateChallengeAsync(request);
                    challenges.Add(challenge);
                    await Task.Delay(1000); // Rate limiting
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating challenge {i + 1}: {ex.Message}");
                }
            }

            return challenges;
        }

        public async Task<string> VerifySubmissionAsync(string imageUrl, string challengeCriteria)
        {
            var prompt = $@"Analyze this image for an ecological challenge verification.

Challenge Criteria: {challengeCriteria}

Please verify if the image shows evidence of completing this challenge. 
Respond with:
1. VALID or INVALID
2. Confidence score (0-100)
3. Brief explanation

Format: {{""status"":""VALID/INVALID"",""confidence"":85,""explanation"":""reason""}}";

            var response = await CallGeminiVisionAPIAsync(prompt, imageUrl);
            return response;
        }

        public async Task<double> AnalyzeSubmissionConfidenceAsync(string imageUrl, ChallengeType type)
        {
            var criteria = GetVerificationCriteriaForType(type);
            var result = await VerifySubmissionAsync(imageUrl, criteria);
            
            try
            {
                var json = JsonDocument.Parse(result);
                if (json.RootElement.TryGetProperty("confidence", out var confidence))
                {
                    return confidence.GetDouble() / 100.0;
                }
            }
            catch
            {
                return 0.5; // Default confidence
            }

            return 0.5;
        }

        private string BuildChallengePrompt(GenerateChallengeRequestDto request)
        {
            var typeStr = request.Type?.ToString() ?? "any ecological activity";
            var difficultyStr = request.Difficulty?.ToString() ?? "medium";
            var locationContext = !string.IsNullOrEmpty(request.UserLocation) 
                ? $" in {request.UserLocation}" 
                : "";
            var seasonContext = !string.IsNullOrEmpty(request.Season) 
                ? $" appropriate for {request.Season}" 
                : "";
            var themeContext = !string.IsNullOrEmpty(request.Theme) 
                ? $" related to {request.Theme}" 
                : "";

            return $@"Generate a creative and engaging ecological challenge for users to complete.

Challenge Type: {typeStr}
Difficulty Level: {difficultyStr}
Location: {locationContext}
Season: {seasonContext}
Theme: {themeContext}

Requirements:
1. Create a specific, actionable challenge that makes a real environmental impact
2. Challenge should be completable with photo/video proof
3. Include clear success criteria for verification
4. Provide 3-5 helpful tips for completing the challenge
5. Suggest 3-5 relevant tags
6. The challenge should be meaningful, not superficial gamification
7. Points should reflect difficulty: Easy (10-30), Medium (31-60), Hard (61-90), Expert (91-150)

Respond ONLY in valid JSON format with this exact structure:
{{
  ""title"": ""Challenge title (max 100 characters)"",
  ""description"": ""Detailed description of what user must do (200-500 words)"",
  ""tips"": ""3-5 actionable tips separated by newlines"",
  ""tags"": [""tag1"", ""tag2"", ""tag3""],
  ""verificationCriteria"": ""Clear criteria for AI/manual verification"",
  ""suggestedPoints"": 50
}}

Make it inspiring and educational!";
        }

        private async Task<string> CallGeminiAPIAsync(string prompt)
        {
            var url = $"{BaseUrl}/gemini-2.0-flash-exp:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.9,
                    topK = 40,
                    topP = 0.95,
                    maxOutputTokens = 2048,
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(responseBody);

            var text = result.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? string.Empty;
        }

        private async Task<string> CallGeminiVisionAPIAsync(string prompt, string imageUrl)
        {
            var url = $"{BaseUrl}/gemini-2.0-flash-exp:generateContent?key={_apiKey}";

            // Download image and convert to base64
            byte[] imageBytes;
            try
            {
                imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
            }
            catch
            {
                return @"{""status"":""INVALID"",""confidence"":0,""explanation"":""Could not load image""}";
            }

            var base64Image = Convert.ToBase64String(imageBytes);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new 
                            { 
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = base64Image
                                }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonDocument.Parse(responseBody);

                var text = result.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return text ?? @"{""status"":""INVALID"",""confidence"":0,""explanation"":""No response""}";
            }
            catch
            {
                return @"{""status"":""INVALID"",""confidence"":0,""explanation"":""Verification failed""}";
            }
        }

        private AIGeneratedChallengeDto ParseChallengeResponse(string response, GenerateChallengeRequestDto request)
        {
            try
            {
                // Clean up response (remove markdown code blocks if present)
                var cleanJson = response.Trim();
                if (cleanJson.StartsWith("```json"))
                {
                    cleanJson = cleanJson.Substring(7);
                }
                if (cleanJson.StartsWith("```"))
                {
                    cleanJson = cleanJson.Substring(3);
                }
                if (cleanJson.EndsWith("```"))
                {
                    cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                }
                cleanJson = cleanJson.Trim();

                var jsonDoc = JsonDocument.Parse(cleanJson);
                var root = jsonDoc.RootElement;

                return new AIGeneratedChallengeDto
                {
                    Title = root.GetProperty("title").GetString() ?? "Eco Challenge",
                    Description = root.GetProperty("description").GetString() ?? "",
                    Tips = root.GetProperty("tips").GetString() ?? "",
                    Tags = root.GetProperty("tags").EnumerateArray()
                        .Select(t => t.GetString() ?? "")
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList(),
                    VerificationCriteria = root.GetProperty("verificationCriteria").GetString() ?? "",
                    SuggestedPoints = root.TryGetProperty("suggestedPoints", out var points) 
                        ? points.GetInt32() 
                        : CalculateDefaultPoints(request.Difficulty)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing AI response: {ex.Message}");
                return CreateFallbackChallenge(request);
            }
        }

        private AIGeneratedChallengeDto CreateFallbackChallenge(GenerateChallengeRequestDto request)
        {
            var type = request.Type ?? ChallengeType.Recycling;
            
            return new AIGeneratedChallengeDto
            {
                Title = $"{type} Challenge",
                Description = "Complete this ecological challenge and upload proof.",
                Tips = "Follow best practices and document your progress.",
                Tags = new List<string> { type.ToString().ToLower(), "ecology", "sustainability" },
                VerificationCriteria = "Clear photo evidence of completed challenge",
                SuggestedPoints = CalculateDefaultPoints(request.Difficulty)
            };
        }

        private int CalculateDefaultPoints(ChallengeDifficulty? difficulty)
        {
            return difficulty switch
            {
                ChallengeDifficulty.Easy => 20,
                ChallengeDifficulty.Medium => 50,
                ChallengeDifficulty.Hard => 75,
                ChallengeDifficulty.Expert => 120,
                _ => 50
            };
        }

        private string GetVerificationCriteriaForType(ChallengeType type)
        {
            return type switch
            {
                ChallengeType.Recycling => "Image must show recyclable items properly sorted",
                ChallengeType.LitterPickup => "Image must show collected litter with visible trash bag or container",
                ChallengeType.Planting => "Image must show planted seeds/plants in soil or pot",
                ChallengeType.PlantIdentification => "Image must clearly show plant with identifiable features",
                ChallengeType.Composting => "Image must show compost bin or pile with organic waste",
                _ => "Image must show clear evidence of ecological action"
            };
        }
    }
}