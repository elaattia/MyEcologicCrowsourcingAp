using Microsoft.EntityFrameworkCore;
using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Data;

namespace MyEcologicCrowsourcingApp.Repositories
{
    public class OrganisationRepository : IOrganisationRepository
    {
        private readonly EcologicDbContext _context;

        public OrganisationRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Organisation>> GetAllAsync()
            => await _context.Organisations
                .Include(o => o.Vehicule) // eager load vehicule if needed
                .ToListAsync();

        public async Task<Organisation?> GetByIdAsync(Guid id)
            => await _context.Organisations
                .Include(o => o.Vehicule)
                .FirstOrDefaultAsync(o => o.OrganisationId == id);

        public async Task AddAsync(Organisation organisation)
        {
            await _context.Organisations.AddAsync(organisation);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Organisation organisation)
        {
            _context.Organisations.Update(organisation);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var organisation = await _context.Organisations.FindAsync(id);
            if (organisation != null)
            {
                _context.Organisations.Remove(organisation);
                await _context.SaveChangesAsync();
            }
        }
    }
}
