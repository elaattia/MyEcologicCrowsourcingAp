using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Services.Interfaces;
using MyEcologicCrowsourcingApp.DTOs;

namespace MyEcologicCrowsourcingApp.Services
{
    public class DepotService : IDepotService
    {
        private readonly IDepotRepository _repository;

        public DepotService(IDepotRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<DepotDto>> GetAllAsync()
        {
            var depots = await _repository.GetAllAsync();
            return depots.Select(d => new DepotDto
            {
                Id = d.Id,
                Nom = d.Nom,
                Latitude = d.Latitude,
                Longitude = d.Longitude,
                Adresse = d.Adresse,
                OrganisationId = d.OrganisationId,
                EstActif = d.EstActif
            });
        }

        public async Task<DepotDto?> GetByIdAsync(Guid id)
        {
            var depot = await _repository.GetByIdAsync(id);
            if (depot == null) return null;

            return new DepotDto
            {
                Id = depot.Id,
                Nom = depot.Nom,
                Latitude = depot.Latitude,
                Longitude = depot.Longitude,
                Adresse = depot.Adresse,
                OrganisationId = depot.OrganisationId,
                EstActif = depot.EstActif
            };
        }

        public async Task<DepotDto> CreateAsync(Depot depot)
        {
            depot.Id = Guid.NewGuid();
            await _repository.AddAsync(depot);
            
            return new DepotDto
            {
                Id = depot.Id,
                Nom = depot.Nom,
                Latitude = depot.Latitude,
                Longitude = depot.Longitude,
                Adresse = depot.Adresse,
                OrganisationId = depot.OrganisationId,
                EstActif = depot.EstActif
            };
        }

        public async Task<DepotDto?> UpdateAsync(Guid id, Depot depot)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Nom = depot.Nom;
            existing.Latitude = depot.Latitude;
            existing.Longitude = depot.Longitude;
            existing.Adresse = depot.Adresse;
            existing.EstActif = depot.EstActif;

            await _repository.UpdateAsync(existing);
            
            return new DepotDto
            {
                Id = existing.Id,
                Nom = existing.Nom,
                Latitude = existing.Latitude,
                Longitude = existing.Longitude,
                Adresse = existing.Adresse,
                OrganisationId = existing.OrganisationId,
                EstActif = existing.EstActif
            };
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