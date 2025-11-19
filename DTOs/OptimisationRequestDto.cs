using System.ComponentModel.DataAnnotations;
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.DTOs
{
    public class OptimisationRequestDto
    {
        [Required]
        public Guid OrganisationId { get; set; }
        
        // Optionnel : permettre de sélectionner des véhicules spécifiques
        public List<Guid>? VehiculesIds { get; set; }
        
        // Optionnel : permettre de sélectionner un dépôt spécifique
        public Guid? DepotId { get; set; }
        
        // Optionnel : temps maximum par trajet
        public TimeSpan? TempsMaxParTrajet { get; set; }
    }

    public class OptimisationResponseDto
    {
        public Guid OptimisationRequestId { get; set; }
        public string DepotUtilise { get; set; } = string.Empty;
        public string DepotAdresse { get; set; } = string.Empty;
        public string ZoneGeographique { get; set; } = string.Empty;
        public int NombreVehicules { get; set; }
        public int NombreItineraires { get; set; }
        public int NombrePointsCollectes { get; set; }
        public double DistanceTotale { get; set; }
        public TimeSpan DureeTotale { get; set; }
        public double CarburantTotal { get; set; }
        public List<ItineraireDto> Itineraires { get; set; } = new();
        public double ScoreEfficacite { get; set; }
    }
      

    public class ItineraireDto
    {
        public Guid Id { get; set; }
        public int VehiculeNumero { get; set; }
        public string VehiculeInfo { get; set; } = string.Empty;
        public string VehiculeType { get; set; } = string.Empty;
        public int NombrePoints { get; set; }
        public double DistanceKm { get; set; }
        public string DureeEstimee { get; set; } = string.Empty;
        public double CarburantLitres { get; set; }
        public List<PointDechetSimpleDto> Points { get; set; } = new();
    }

    public class PointDechetSimpleDto
    {
        public Guid Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Type { get; set; }
        public double Volume { get; set; }
        public string? Zone { get; set; }
    }
    
    
}