namespace MyEcologicCrowsourcingApp.Models
{
    public class OptimisationRequest
    {
        public Guid Id { get; set; }
        public Guid OrganisationId { get; set; }
        public List<Guid> ListePointsIds { get; set; } = new();
        public double CapaciteVehicule { get; set; }
        public TimeSpan? TempsMaxParTrajet { get; set; }
        public string ZoneGeographique { get; set; } = string.Empty;

        public Organisation? Organisation { get; set; }
    }
}
