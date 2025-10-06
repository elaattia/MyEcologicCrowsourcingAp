namespace MyEcologicCrowsourcingApp.Models
{
    public class Itineraire
    {
        public Guid Id { get; set; }
        public List<PointDechet> ListePoints { get; set; } = new();
        public double DistanceTotale { get; set; }
        public Guid OrganisationId { get; set; }
        public TimeSpan DureeEstimee { get; set; }
        public double CarburantEstime { get; set; }

        public Organisation? Organisation { get; set; }
    }
}
