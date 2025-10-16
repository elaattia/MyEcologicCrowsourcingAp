namespace MyEcologicCrowsourcingApp.Models
{
    public enum TypeVehicule
    {
        Camion,
        Camionette,
        Voiture,
        VeloCargo
    }

    public class Vehicule
    {
        public Guid Id { get; set; }
        
        public string Immatriculation { get; set; } = string.Empty;

        public TypeVehicule Type { get; set; }
        public double CapaciteMax { get; set; } // en m³
        public double VitesseMoyenne { get; set; } // km/h
        public double CarburantConsommation { get; set; } // L/100km

        public Guid OrganisationId { get; set; }
        public Organisation? Organisation { get; set; }
        
        public bool EstDisponible { get; set; } = true;
        public DateTime? DerniereUtilisation { get; set; }
    }
}
