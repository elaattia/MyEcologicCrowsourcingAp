namespace MyEcologicCrowsourcingApp.Models
{
    public enum StatutItineraire
    {
        EnAttente,      
        EnCours,      
        Termine,      
        Annule          
    }

    public class Itineraire
    {
        public Guid Id { get; set; }
        public List<PointDechet> ListePoints { get; set; } = new();
        public double DistanceTotale { get; set; }
        public Guid OrganisationId { get; set; }
        public TimeSpan DureeEstimee { get; set; }
        public double CarburantEstime { get; set; }

        public Organisation? Organisation { get; set; }

        public StatutItineraire Statut { get; set; } = StatutItineraire.EnAttente;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
    }
}
