namespace MyEcologicCrowsourcingApp.Models
{
    public class OptimisationResult
    {
        public Guid Id { get; set; }
        public Guid OptimisationRequestId { get; set; }
        public Itineraire? ItineraireOptimisee { get; set; }
        public double ScoreEfficacite { get; set; }

        public OptimisationRequest? OptimisationRequest { get; set; }
    }
}
