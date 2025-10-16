using MyEcologicCrowsourcingApp.Models;
using MyEcologicCrowsourcingApp.Repositories.Interfaces;
using MyEcologicCrowsourcingApp.Services.Interfaces;

namespace MyEcologicCrowsourcingApp.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<User>> GetAllAsync() => await _repository.GetAllAsync();

        public async Task<User?> GetByIdAsync(Guid id) => await _repository.GetByIdAsync(id);

        public async Task<User> CreateAsync(User user)
        {
            user.UserId = Guid.NewGuid();
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            if (user.Role == UserRole.Representant && user.OrganisationId == null)
                throw new ArgumentException("Un représentant doit être lié à une organisation.");


            await _repository.AddAsync(user);
            return user;
        }

        public async Task<User?> UpdateAsync(Guid id, User user)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Username = user.Username;
            existing.Email = user.Email;
            existing.Password = user.Password;
            existing.Role = user.Role;

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

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var user = await _repository.GetByEmailAsync(email);

            if (user == null) return null;

            bool isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.Password);


            return isValidPassword ? user : null;
        }
        
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _repository.GetByEmailAsync(email);
        }
    }
}