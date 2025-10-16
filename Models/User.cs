////Models/User.cs
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyEcologicCrowsourcingApp.Models
{
    public enum UserRole
    {
        User,
        Admin
    }

    public class User
    {
        public Guid UserId { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        [MinLength(3)]
        public string Username { get; set; } = string.Empty;
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty; 
        [Required]
        public UserRole Role { get; set; }
    }
}
