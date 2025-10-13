using MyEcologicCrowsourcingApp.Models;

namespace MyEcologicCrowsourcingApp.DTOs
{
    public class PointDechetFilterRequest
    {
        public string? Search { get; set; } 
        public StatutDechet? Statut { get; set; }
        public TypeDechet? Type { get; set; }
        public string? Pays { get; set; }
        public string? Zone { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public Guid? UserId { get; set; } 
        
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        
        public string? SortBy { get; set; } = "Date"; 
        public bool Descending { get; set; } = true;
    }

    public class PointDechetResponse
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string? Type { get; set; }
        public DateTime Date { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string Zone { get; set; } = string.Empty;
        public string Pays { get; set; } = string.Empty;
        public double? VolumeEstime { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}