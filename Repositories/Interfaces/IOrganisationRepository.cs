using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IOrganisationRepository
    {
        Task<IEnumerable<Organisation>> GetAllAsync();
        Task<Organisation?> GetByIdAsync(Guid id);
        Task AddAsync(Organisation organisation);
        Task UpdateAsync(Organisation organisation);
        Task DeleteAsync(Guid id);
    }
}
