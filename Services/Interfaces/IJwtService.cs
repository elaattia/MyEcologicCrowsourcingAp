//Services/Interfaces/IJwtService.cs
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}