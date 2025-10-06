using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IVehiculeService
    {
        Task<IEnumerable<Vehicule>> GetAllAsync();
        Task<Vehicule?> GetByIdAsync(Guid id);
        Task<Vehicule> CreateAsync(Vehicule vehicule);
        Task<Vehicule?> UpdateAsync(Guid id, Vehicule vehicule);
        Task<bool> DeleteAsync(Guid id);
    }
}
