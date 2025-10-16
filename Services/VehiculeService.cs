using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Services.Interfaces;
using MyEcologicCrowsourcingApp.DTOs;

namespace MyEcologicCrowsourcingApp.Services
{
    public class VehiculeService : IVehiculeService
    {
        private readonly IVehiculeRepository _repository;

        public VehiculeService(IVehiculeRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<VehiculeDto>> GetAllAsync()
        {
            var vehicules = await _repository.GetAllAsync();
            return vehicules.Select(v => new VehiculeDto
            {
                Id = v.Id,
                Immatriculation = v.Immatriculation,
                Type = v.Type.ToString(),
                CapaciteMax = v.CapaciteMax,
                VitesseMoyenne = v.VitesseMoyenne,
                CarburantConsommation = v.CarburantConsommation,
                OrganisationId = v.OrganisationId,
                EstDisponible = v.EstDisponible,
                DerniereUtilisation = v.DerniereUtilisation
            });
        }

        public async Task<VehiculeDto?> GetByIdAsync(Guid id)
        {
            var vehicule = await _repository.GetByIdAsync(id);
            if (vehicule == null) return null;

            return new VehiculeDto
            {
                Id = vehicule.Id,
                Immatriculation = vehicule.Immatriculation,
                Type = vehicule.Type.ToString(),
                CapaciteMax = vehicule.CapaciteMax,
                VitesseMoyenne = vehicule.VitesseMoyenne,
                CarburantConsommation = vehicule.CarburantConsommation,
                OrganisationId = vehicule.OrganisationId,
                EstDisponible = vehicule.EstDisponible,
                DerniereUtilisation = vehicule.DerniereUtilisation
            };
        }

        public async Task<VehiculeDto> CreateAsync(Vehicule vehicule)
        {
            vehicule.Id = Guid.NewGuid();
            await _repository.AddAsync(vehicule);
            
            return new VehiculeDto
            {
                Id = vehicule.Id,
                Immatriculation = vehicule.Immatriculation,
                Type = vehicule.Type.ToString(),
                CapaciteMax = vehicule.CapaciteMax,
                VitesseMoyenne = vehicule.VitesseMoyenne,
                CarburantConsommation = vehicule.CarburantConsommation,
                OrganisationId = vehicule.OrganisationId,
                EstDisponible = vehicule.EstDisponible,
                DerniereUtilisation = vehicule.DerniereUtilisation
            };
        }

        public async Task<VehiculeDto?> UpdateAsync(Guid id, Vehicule vehicule)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Immatriculation = vehicule.Immatriculation;
            existing.Type = vehicule.Type;
            existing.CapaciteMax = vehicule.CapaciteMax;
            existing.VitesseMoyenne = vehicule.VitesseMoyenne;
            existing.CarburantConsommation = vehicule.CarburantConsommation;
            existing.EstDisponible = vehicule.EstDisponible;
            existing.DerniereUtilisation = vehicule.DerniereUtilisation;

            await _repository.UpdateAsync(existing);
            
            return new VehiculeDto
            {
                Id = existing.Id,
                Immatriculation = existing.Immatriculation,
                Type = existing.Type.ToString(),
                CapaciteMax = existing.CapaciteMax,
                VitesseMoyenne = existing.VitesseMoyenne,
                CarburantConsommation = existing.CarburantConsommation,
                OrganisationId = existing.OrganisationId,
                EstDisponible = existing.EstDisponible,
                DerniereUtilisation = existing.DerniereUtilisation
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