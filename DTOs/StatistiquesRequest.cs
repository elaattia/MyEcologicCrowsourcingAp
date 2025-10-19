using MyEcologicCrowsourcingApp.Models;
namespace MyEcologicCrowsourcingApp.DTOs
{
    public class StatistiquesRequest
    {
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public string? Pays { get; set; }
        public string? Zone { get; set; }
        public Guid? UserId { get; set; }
    }

    public class StatistiquesResponse
    {
        public int TotalDechets { get; set; }
        public int TotalSignales { get; set; }
        public int TotalNettoyés { get; set; }
        public double PourcentageNettoyage { get; set; }
        public List<StatParStatut> ParStatut { get; set; } = new();
        public List<StatParType> ParType { get; set; } = new();
        public List<StatParZone> ParZone { get; set; } = new();
        public List<StatParPays> ParPays { get; set; } = new();
        public List<StatParDate> ParDate { get; set; } = new();

        public List<StatParUtilisateur> TopUtilisateurs { get; set; } = new();
    }

    public class StatParStatut
    {
        public string Statut { get; set; } = string.Empty;
        public int Nombre { get; set; }
        public double Pourcentage { get; set; }
    }

    public class StatParType
    {
        public string Type { get; set; } = string.Empty;
        public int Nombre { get; set; }
        public double Pourcentage { get; set; }
    }

    public class StatParZone
    {
        public string Zone { get; set; } = string.Empty;
        public int Nombre { get; set; }
        public int Signales { get; set; }
        public int Nettoyés { get; set; }
        public double PourcentageNettoyage { get; set; }
    }

    public class StatParPays
    {
        public string Pays { get; set; } = string.Empty;
        public int Nombre { get; set; }
        public int Signales { get; set; }
        public int Nettoyés { get; set; }
        public double PourcentageNettoyage { get; set; }
    }

    public class StatParDate
    {
        public DateTime Date { get; set; }
        public int Nombre { get; set; }
        public int Signales { get; set; }
        public int Nettoyés { get; set; }
    }

    public class StatParUtilisateur
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int NombreDechetsSignales { get; set; }
        public int NombreDechetsNettoyés { get; set; }
    }
}