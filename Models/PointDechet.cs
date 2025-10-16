////Models/PonitDeche.cs
using System.ComponentModel.DataAnnotations;

namespace MyEcologicCrowsourcingApp.Models
{
    public enum StatutDechet
    {
        Signale,
        Nettoye
    }

    public enum TypeDechet
    {
        Plastique,
        Verre,
        Metale,
        Pile,
        Papier,
        Autre
    }

    public class PointDechet
    {
        public Guid Id { get; set; }
        
        [Required]
        public string Url { get; set; } = string.Empty; 
        
        [Required]
        [Range(-90, 90)]
        public double Latitude { get; set; }
        
        [Required]
        [Range(-180, 180)]
        public double Longitude { get; set; }
        
        [Required]
        public StatutDechet Statut { get; set; } = StatutDechet.Signale;
        
        public TypeDechet? Type { get; set; }
        
        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;
        
        [Required]
        public Guid UserId { get; set; }

        public User? User { get; set; }
        
        public string Zone { get; set; } = string.Empty;
        public string Pays { get; set; } = string.Empty;
        
        public double? VolumeEstime { get; set; }

    }
}