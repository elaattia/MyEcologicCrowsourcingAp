//Services/Interfaces/IUserService.cs
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> CreateAsync(User user);
        Task<User?> UpdateAsync(Guid id, User user);
        Task<bool> DeleteAsync(Guid id);
        Task<User?> AuthenticateAsync(string email, string password);
    }
}