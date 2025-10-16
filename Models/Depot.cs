namespace MyEcologicCrowsourcingApp.Models
{
    public class Depot
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Adresse { get; set; } = string.Empty;
        public Guid OrganisationId { get; set; }
        public Organisation? Organisation { get; set; }
        public bool EstActif { get; set; } = true;
    }
}