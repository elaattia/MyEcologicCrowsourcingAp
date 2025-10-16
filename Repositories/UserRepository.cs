using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Data;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MyEcologicCrowsourcingApp.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly EcologicDbContext _context;

        public UserRepository(EcologicDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync() 
            => await _context.Users.ToListAsync();

        public async Task<User?> GetByIdAsync(Guid id) 
            => await _context.Users.FindAsync(id);

        public async Task<User?> GetByEmailAsync(string email) 
            => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);


        public async Task AddAsync(User user)
        {
            Console.WriteLine($"UserRepository.AddAsync - UserId: {user.UserId}, Email: {user.Email}");
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            Console.WriteLine("SaveChanges appelé pour User");
        }

        public async Task UpdateAsync(User user)
        {
            Console.WriteLine($"UserRepository.UpdateAsync - UserId: {user.UserId}, OrgId: {user.OrganisationId}");
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            Console.WriteLine("SaveChanges appelé pour User update");
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
    }
}