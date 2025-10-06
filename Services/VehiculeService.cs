using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Services.Interfaces;
using BCrypt.Net;
namespace MyEcologicCrowsourcingApp.Services
{
    public class VehiculeService : IVehiculeService
    {
        private readonly IVehiculeRepository _repository;

        public VehiculeService(IVehiculeRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Vehicule>> GetAllAsync() => await _repository.GetAllAsync();

        public async Task<Vehicule?> GetByIdAsync(Guid id) => await _repository.GetByIdAsync(id);

        public async Task<Vehicule> CreateAsync(Vehicule vehicule)
        {
            vehicule.Id = Guid.NewGuid();
            await _repository.AddAsync(vehicule);
            return vehicule;
        }

        public async Task<Vehicule?> UpdateAsync(Guid id, Vehicule vehicule)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Type = vehicule.Type;
            existing.CapaciteMax = vehicule.CapaciteMax;
            existing.VitesseMoyenne = vehicule.VitesseMoyenne;
            existing.CarburantConsommation = vehicule.CarburantConsommation;

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
