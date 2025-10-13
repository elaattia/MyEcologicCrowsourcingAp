using System.ComponentModel.DataAnnotations;

namespace MyEcologicCrowsourcingApp.Models
{
    public class ClassifyWasteRequest
    {
        [Required]
        public Guid PointDechetId { get; set; }
    }
}