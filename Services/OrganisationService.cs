using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Services
{
    public class OrganisationService : IOrganisationService
    {
        private readonly IOrganisationRepository _repository;

        public OrganisationService(IOrganisationRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Organisation>> GetAllAsync() => await _repository.GetAllAsync();

        public async Task<Organisation?> GetByIdAsync(Guid id) => await _repository.GetByIdAsync(id);

        public async Task<Organisation> CreateAsync(Organisation organisation)
        {
            organisation.OrganisationId = Guid.NewGuid();
            await _repository.AddAsync(organisation);
            return organisation;
        }

        public async Task<Organisation?> UpdateAsync(Guid id, Organisation organisation)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Nom = organisation.Nom;
            existing.VehiculeId = organisation.VehiculeId;
            existing.NbrVolontaires = organisation.NbrVolontaires;

            await _repository.UpdateAsync(existing);
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return false;

            await _repository.DeleteAsync(id);
            return true;
        }
    }
}
