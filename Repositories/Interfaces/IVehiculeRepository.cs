//Repositories/Interfaces/IVehiculeRepository.cs
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.Repositories.Interfaces
{
    public interface IVehiculeRepository
    {
        Task<IEnumerable<Vehicule>> GetAllAsync();
        Task<Vehicule?> GetByIdAsync(Guid id);
        Task AddAsync(Vehicule vehicule);
        Task UpdateAsync(Vehicule vehicule);
        Task DeleteAsync(Guid id);
    }
}
