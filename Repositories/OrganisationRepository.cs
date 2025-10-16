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
                .ToListAsync();

        public async Task<Organisation?> GetByIdAsync(Guid id)
        {
            Console.WriteLine($"Recherche organisation avec ID: {id}");
            
            var org = await _context.Organisations
                .AsNoTracking() // Ajoutez ceci pour éviter les problèmes de tracking
                .FirstOrDefaultAsync(o => o.OrganisationId == id);
            
            Console.WriteLine($"Organisation trouvée: {org != null}");
            
            if (org != null)
            {
                Console.WriteLine($"Nom: {org.Nom}, RepresentantId: {org.RepresentantId}");
            }
            
            return org;
        }
        public async Task AddAsync(Organisation organisation)
        {
            Console.WriteLine($"OrganisationRepository.AddAsync - OrgId: {organisation.OrganisationId}, Nom: {organisation.Nom}");
            await _context.Organisations.AddAsync(organisation);
            await _context.SaveChangesAsync();
            Console.WriteLine("SaveChanges appelé pour Organisation");
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
