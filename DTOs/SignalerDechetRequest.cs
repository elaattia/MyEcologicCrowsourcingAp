// DTOs/SignalerDechetRequest.cs
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace MyEcologicCrowsourcingApp.DTOs
{      
    public class SignalerDechetRequest
    {
        [Required]
        public IFormFile Image { get; set; } = null!;
        
        [Required]
        public string Latitude { get; set; } = string.Empty;
        
        [Required]
        public string Longitude { get; set; } = string.Empty;

        public double GetLatitude()
        {
            return double.Parse(Latitude, CultureInfo.InvariantCulture);
        }

        public double GetLongitude()
        {
            return double.Parse(Longitude, CultureInfo.InvariantCulture);
        }
    }
}