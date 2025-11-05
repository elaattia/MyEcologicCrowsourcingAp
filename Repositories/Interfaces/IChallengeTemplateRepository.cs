using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IChallengeTemplateRepository
    {
        Task<IEnumerable<ChallengeTemplate>> GetAllAsync();
        Task<ChallengeTemplate?> GetByIdAsync(Guid id);
        Task<IEnumerable<ChallengeTemplate>> GetByTypeAsync(ChallengeType type);
        Task<ChallengeTemplate?> GetRandomByTypeAsync(ChallengeType type);
        Task<ChallengeTemplate> CreateAsync(ChallengeTemplate template);
        Task<ChallengeTemplate> UpdateAsync(ChallengeTemplate template);
        Task<bool> DeleteAsync(Guid id);
    }
}