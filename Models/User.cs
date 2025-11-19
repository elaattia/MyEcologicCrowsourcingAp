using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyEcologicCrowsourcingApp.Models
{
    public enum UserRole
    {
        User,
        Representant,
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
        
        public Guid? OrganisationId { get; set; }  
        public Organisation? Organisation { get; set; }

        [JsonIgnore]
        public UserStats? Stats { get; set; }

        [JsonIgnore]
        public ICollection<UserChallenge> UserChallenges { get; set; } = new List<UserChallenge>();

        [JsonIgnore]
        public ICollection<ChallengeSubmission> Submissions { get; set; } = new List<ChallengeSubmission>();

        [JsonIgnore]
        public ICollection<UserAchievement> Achievements { get; set; } = new List<UserAchievement>();
    }
}
