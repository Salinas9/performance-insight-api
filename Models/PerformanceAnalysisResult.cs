namespace PerformanceInsight.Api.Models;

public class PerformanceAnalysisResult
{
    public int TotalSamples { get; set; }
    public double AverageMs { get; set; }
    public double MinMs { get; set; }
    public double MaxMs { get; set; }
    public double P95Ms { get; set; }
    public double ErrorRate { get; set; }
    public string SlowestEndpoint { get; set; } = "";
    public string Status { get; set; } = "";
    public List<string> Recommendations { get; set; } = new();
}