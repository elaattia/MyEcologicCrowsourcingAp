using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IDepotRepository
    {
        Task<IEnumerable<Depot>> GetAllAsync();
        Task<Depot?> GetByIdAsync(Guid id);
        Task AddAsync(Depot depot);
        Task UpdateAsync(Depot depot);
        Task DeleteAsync(Guid id);
    }
}
