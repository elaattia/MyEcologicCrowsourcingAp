namespace MyEcologicCrowsourcingApp.Models
{
    public class Organisation
    {
        public Guid OrganisationId { get; set; }
        public string Nom { get; set; } = string.Empty;
        public Guid? VehiculeId { get; set; }
        public int NbrVolontaires { get; set; }

        public Vehicule? Vehicule { get; set; }
        public ICollection<Itineraire>? Itineraires { get; set; }
    }
}
