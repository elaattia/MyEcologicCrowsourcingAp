//Services/Interfaces/IOrganisationService.cs
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IOrganisationService
    {
        Task<IEnumerable<Organisation>> GetAllAsync();
        Task<Organisation?> GetByIdAsync(Guid id);
        Task<Organisation> CreateAsync(Organisation organisation);
        Task<Organisation?> UpdateAsync(Guid id, Organisation organisation);
        Task<bool> DeleteAsync(Guid id);
    }
}
