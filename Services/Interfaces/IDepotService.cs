using MyEcologicCrowsourcingApp.DTOs;
using MyEcologicCrowsourcingApp.Models;


namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IDepotService
    {
        Task<IEnumerable<DepotDto>> GetAllAsync();
        Task<DepotDto?> GetByIdAsync(Guid id);
        Task<DepotDto> CreateAsync(Depot depot);
        Task<DepotDto?> UpdateAsync(Guid id, Depot depot);
        Task<bool> DeleteAsync(Guid id);
    }
}