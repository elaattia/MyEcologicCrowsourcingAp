using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Data;

namespace MyEcologicCrowsourcingApp.Repositories
{
    public class DepotRepository : IDepotRepository
    {
        private readonly EcologicDbContext _context;

        public DepotRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Depot>> GetAllAsync()
            => await _context.Depots.Include(d => d.Organisation).ToListAsync();

        public async Task<Depot?> GetByIdAsync(Guid id)
            => await _context.Depots.Include(d => d.Organisation).FirstOrDefaultAsync(d => d.Id == id);

        public async Task AddAsync(Depot depot)
        {
            await _context.Depots.AddAsync(depot);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Depot depot)
        {
            _context.Depots.Update(depot);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var depot = await _context.Depots.FindAsync(id);
            if (depot != null)
            {
                _context.Depots.Remove(depot);
                await _context.SaveChangesAsync();
            }
        }
    }
}
