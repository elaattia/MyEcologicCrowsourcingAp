using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs.Challenge;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IAchievementRepository
    {
        Task<IEnumerable<Achievement>> GetAllAsync();
        Task<Achievement?> GetByIdAsync(Guid id);
        Task<IEnumerable<Achievement>> GetActiveAsync();
        Task<Achievement> CreateAsync(Achievement achievement);
        Task<Achievement> UpdateAsync(Achievement achievement);
        Task<bool> DeleteAsync(Guid id);
    }
}