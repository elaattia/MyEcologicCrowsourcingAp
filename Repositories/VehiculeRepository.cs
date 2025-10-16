//Repositories/VehiculeRepository.cs
using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Data;

namespace MyEcologicCrowsourcingApp.Repositories
{
    public class VehiculeRepository : IVehiculeRepository
    {
        private readonly EcologicDbContext _context;

        public VehiculeRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Vehicule>> GetAllAsync()
            => await _context.Vehicules.ToListAsync();

        public async Task<Vehicule?> GetByIdAsync(Guid id)
            => await _context.Vehicules.FindAsync(id);

        public async Task AddAsync(Vehicule vehicule)
        {
            await _context.Vehicules.AddAsync(vehicule);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Vehicule vehicule)
        {
            _context.Vehicules.Update(vehicule);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var vehicule = await _context.Vehicules.FindAsync(id);
            if (vehicule != null)
            {
                _context.Vehicules.Remove(vehicule);
                await _context.SaveChangesAsync();
            }
        }
    }
}
