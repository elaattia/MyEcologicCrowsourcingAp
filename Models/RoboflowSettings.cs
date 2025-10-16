////Models/RoboflowSttings.cs
using System.Text.Json.Serialization;

namespace MyEcologicCrowsourcingApp.Models
{
    
    public class RoboflowSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public double ConfidenceThreshold { get; set; } = 0.40;
        public string ApiEndpoint => $"https://detect.roboflow.com/{ModelId}/{Version}";
    }

    public class WasteClassificationResponse
    {
        public string Category { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string RoboflowClass { get; set; } = string.Empty;
        public int DetectedObjects { get; set; }
        public double ProcessingTime { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string? FileName { get; set; }
        public List<Detection> AllDetections { get; set; } = new();
        public bool Success { get; set; } = true;
        public bool FromCache { get; set; } = false;
    }

    public class Detection
    {
        public string Class { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public class RoboflowApiResponse
    {
        [JsonPropertyName("time")]
        public double Time { get; set; }

        [JsonPropertyName("image")]
        public RoboflowImage Image { get; set; } = new();

        [JsonPropertyName("predictions")]
        public List<RoboflowPrediction> Predictions { get; set; } = new();
    }

    public class RoboflowImage
    {
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class RoboflowPrediction
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("width")]
        public double Width { get; set; }

        [JsonPropertyName("height")]
        public double Height { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("class")]
        public string Class { get; set; } = string.Empty;

        [JsonPropertyName("class_id")]
        public int ClassId { get; set; }
    }

    public class CategoryInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] RoboflowClasses { get; set; } = Array.Empty<string>();
        public string RecyclingInfo { get; set; } = string.Empty;
    }

    public class BatchClassificationResponse
    {
        public int TotalImages { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int CachedCount { get; set; }
        public double TotalProcessingTime { get; set; }
        public Dictionary<string, int> CategoryStatistics { get; set; } = new();
        public List<WasteClassificationResponse> Results { get; set; } = new();
    }
}