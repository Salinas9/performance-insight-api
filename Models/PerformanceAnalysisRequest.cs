namespace PerformanceInsight.Api.Models;

public class PerformanceAnalysisRequest
{
    public List<PerformanceSample> Samples { get; set; } = new();
}