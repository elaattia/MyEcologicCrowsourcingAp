using MyEcologicCrowsourcingApp.DTOs;
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IVehiculeService
    {
        Task<IEnumerable<VehiculeDto>> GetAllAsync();
        Task<VehiculeDto?> GetByIdAsync(Guid id);
        Task<VehiculeDto> CreateAsync(Vehicule vehicule);
        Task<VehiculeDto?> UpdateAsync(Guid id, Vehicule vehicule);
        Task<bool> DeleteAsync(Guid id);
    }
}