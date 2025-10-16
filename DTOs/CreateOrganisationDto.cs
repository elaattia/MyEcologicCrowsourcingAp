using MyEcologicCrowsourcingApp.Models;
using System.ComponentModel.DataAnnotations;

namespace MyEcologicCrowsourcingApp.DTOs
{
    public class DepotDto
    {
        public Guid Id { get; set; }
        public string? Nom { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Adresse { get; set; }
        public Guid OrganisationId { get; set; }
        public bool EstActif { get; set; }
    }
    public class VehiculeDto
    {
        public Guid Id { get; set; }
        public string Immatriculation { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double CapaciteMax { get; set; }
        public double VitesseMoyenne { get; set; }
        public double CarburantConsommation { get; set; }
        public Guid OrganisationId { get; set; }
        public bool EstDisponible { get; set; }
        public DateTime? DerniereUtilisation { get; set; }
    }
    public class CreateOrganisationDto
    {
        public string Nom { get; set; } = string.Empty;
        public int NbrVolontaires { get; set; }

        public string RepreUsername { get; set; } = string.Empty;
        public string RepreEmail { get; set; } = string.Empty;
        public string ReprePassword { get; set; } = string.Empty;
    }

    public class OrganisationResponseDto
    {
        public Guid OrganisationId { get; set; }
        public string? Nom { get; set; } 
        public int NbrVolontaires { get; set; }
    }

    public class OrganisationDto
    {
        public Guid OrganisationId { get; set; }
        public string? Nom { get; set; } 
        public int NbrVolontaires { get; set; }
    }

    public class OrganisationDetailDto
    {
        public Guid OrganisationId { get; set; }
        public string? Nom { get; set; }
        public int NbrVolontaires { get; set; }
        public Guid? RepresentantId { get; set; }
    }

    public class OrganisationCreationResult
    {
        public Organisation? Organisation { get; set; } = default!;
        public string Token { get; set; } = string.Empty;
    }

    public class CreateVehiculeDto
    {
        public string? Immatriculation { get; set; }
        public TypeVehicule Type { get; set; }
        public double CapaciteMax { get; set; }
        public double VitesseMoyenne { get; set; } = 30.0;
        public double CarburantConsommation { get; set; } = 8.0;
        public Guid OrganisationId { get; set; }
    }

    public class CreateDepotDto
    {
        public string? Nom { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Adresse { get; set; }
        public Guid OrganisationId { get; set; }
    }

    public class OrganisationCreationResponseDto
    {
        public Guid OrganisationId { get; set; }
        public string? Nom { get; set; }
        public int NbrVolontaires { get; set; }
        public string? Token { get; set; }
    }
    public class UpdateOrganisationDto
    {
        [Required(ErrorMessage = "Le nom de l'organisation est requis.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caractères.")]
        public string? Nom { get; set; }

        [Required(ErrorMessage = "Le nombre de volontaires est requis.")]
        [Range(0, int.MaxValue, ErrorMessage = "Le nombre de volontaires doit être positif.")]
        public int NbrVolontaires { get; set; }
    }
}

