namespace MyEcologicCrowsourcingApp.Models
{
    public class Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Name { get; set; } = "Dépôt";
    }

    public class VRPSolution
    {
        public double TotalDistance { get; set; }
        public List<VehicleRoute> Routes { get; set; } = new();
    }

    public class VehicleRoute
    {
        public int VehicleId { get; set; }
        public List<int> PointIndices { get; set; } = new();
        public long Distance { get; set; }
    }

    public class OSRMTableResponse
    {
        public double[][]? distances { get; set; }
        public double[][]? durations { get; set; }
    }

    public class OSRMRouteResponse
    {
        public OSRMRoute[]? routes { get; set; }
    }

    public class OSRMRoute
    {
        public double distance { get; set; } // en mètres
        public double duration { get; set; } // en secondes
        public OSRMGeometry? geometry { get; set; }
        public OSRMStep[]? legs { get; set; }
    }

    public class OSRMGeometry
    {
        public string? type { get; set; }
        public double[][]? coordinates { get; set; }
    }

    public class OSRMStep
    {
        public double distance { get; set; }
        public double duration { get; set; }
    }
}