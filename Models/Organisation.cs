namespace MyEcologicCrowsourcingApp.Models
{
    public class Organisation
    {
        public Guid OrganisationId { get; set; }
        
        public string Nom { get; set; } = string.Empty;
        public Guid? VehiculeId { get; set; }
        public int NbrVolontaires { get; set; }

        public Guid? RepresentantId { get; set; }
        public User? Representant { get; set; }

        public ICollection<User>? Users { get; set; }

        public ICollection<Vehicule>? Vehicules { get; set; } 
        public ICollection<Depot>? Depots { get; set; } 
        public ICollection<Itineraire>? Itineraires { get; set; }
    }
}
