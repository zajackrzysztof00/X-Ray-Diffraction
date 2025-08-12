using System.Text.Json.Serialization;

namespace XRayApi.Models
{
    public class AnalysisResult
    {
        [JsonPropertyName("xData")]
        public double[] XData { get; set; } = null!;

        [JsonPropertyName("yData")]
        public double[] YData { get; set; } = null!;
    }
}
