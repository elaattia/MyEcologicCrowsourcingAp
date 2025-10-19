using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyEcologicCrowsourcingApp.Models
{
    public class RecommandationEcologique
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PointDechetId { get; set; }

        [ForeignKey(nameof(PointDechetId))]
        public PointDechet? PointDechet { get; set; }

        [Required]
        [Range(0, 100)]
        public int ScorePriorite { get; set; }

        [Required]
        [MaxLength(50)]
        public string Urgence { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string ActionRecommandee { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Justification { get; set; } = string.Empty;

        [Required]
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;

        public string? ContexteUtilise { get; set; }

        public bool EstActive { get; set; } = true;
    }


    public class ContexteRecommandation
    {
        public PointDechet PointDechet { get; set; } = null!;
        public int NombreDechetsProches { get; set; }
        public List<DateTime> HistoriqueNettoyages { get; set; } = new();
        public bool OrganisationLocaleActive { get; set; }
        public string Saison { get; set; } = string.Empty;
    }

    public class StatistiquesZone
    {
        public string Zone { get; set; } = string.Empty;
        public DateTime PeriodeDebut { get; set; }
        public DateTime PeriodeFin { get; set; }
        public int NombreTotalSignalements { get; set; }
        public int NombreNettoyes { get; set; }
        public List<TypeDechetStat> TypesPlusFrequents { get; set; } = new();
        public double VolumeTotal { get; set; }
    }

    public class TypeDechetStat
    {
        public TypeDechet Type { get; set; }
        public int Nombre { get; set; }
        public double Pourcentage { get; set; }
    }
}