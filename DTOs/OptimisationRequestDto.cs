using System.ComponentModel.DataAnnotations;
using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.DTOs
{
    public class OptimisationRequestDto
    {
        public Guid OrganisationId { get; set; }
        public int NombreVehicules { get; set; } = 1;
        public double CapaciteVehicule { get; set; } = 10.0; // m³
        public TimeSpan TempsMaxParTrajet { get; set; } = TimeSpan.FromHours(8);
        public string ZoneGeographique { get; set; } = "";
        public double? DepotLatitude { get; set; }
        public double? DepotLongitude { get; set; }
    }

    public class OptimisationResponseDto
    {
        public Guid OptimisationRequestId { get; set; }
        public int NombreItineraires { get; set; }
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
        public int NombrePoints { get; set; }
        public double DistanceKm { get; set; }
        public string DureeEstimee { get; set; }
        public double CarburantLitres { get; set; }
        public List<PointDechetSimpleDto> Points { get; set; } = new();
    }

    public class PointDechetSimpleDto
    {
        public Guid Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Type { get; set; }
        public double Volume { get; set; }
        public string Zone { get; set; }
    }
}