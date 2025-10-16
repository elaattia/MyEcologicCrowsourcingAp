using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.DTOs;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IOrganisationService
    {
        Task<IEnumerable<OrganisationDto>> GetAllAsync();
        Task<OrganisationDetailDto?> GetByIdAsync(Guid id);
        Task<OrganisationCreationResult> CreateWithRepresentativeAsync(Organisation organisation, User representant);
        Task<OrganisationDto?> UpdateAsync(Guid id, Organisation organisation);
        Task<bool> DeleteAsync(Guid id);
    }
}