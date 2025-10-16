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

        public async Task<User?> GetByEmailAsync(string email) =>
            await _repository.GetByEmailAsync(email);

        public async Task<User> CreateAsync(User user)
        {
            // ✅ Vérifie si l'email existe déjà
            var existing = await _repository.GetByEmailAsync(user.Email);
            if (existing != null)
                throw new InvalidOperationException("Cet email est déjà utilisé. Veuillez en choisir un autre.");

            // ✅ Hash du mot de passe
            user.UserId = Guid.NewGuid();
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            await _repository.AddAsync(user);
            return user;
        }

        public async Task<User?> UpdateAsync(Guid id, User user)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Utilisateur introuvable.");

            // ✅ Vérifie si l'email existe déjà pour un autre utilisateur
            var emailOwner = await _repository.GetByEmailAsync(user.Email);
            if (emailOwner != null && emailOwner.UserId != id)
                throw new InvalidOperationException("Cet email est déjà associé à un autre compte.");

            existing.Username = user.Username;
            existing.Email = user.Email;
            existing.Role = user.Role;

            // ✅ Si un nouveau mot de passe est envoyé, on le hash
            if (!string.IsNullOrWhiteSpace(user.Password) &&
                !BCrypt.Net.BCrypt.Verify(user.Password, existing.Password))
            {
                existing.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            }

            await _repository.UpdateAsync(existing);
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Utilisateur introuvable.");

            await _repository.DeleteAsync(id);
            return true;
        }

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var user = await _repository.GetByEmailAsync(email);

            if (user == null)
                throw new UnauthorizedAccessException("Adresse e-mail introuvable.");

            bool isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.Password);

            if (!isValidPassword)
                throw new UnauthorizedAccessException("Mot de passe incorrect.");

            return user;
        }
    }
}
