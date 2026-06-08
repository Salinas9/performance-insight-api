namespace PerformanceInsight.Api.Models;

public class PerformanceReportSnapshot
{
    public string SourceType { get; set; } = "";
    public string SourceName { get; set; } = "";
    public DateTimeOffset ImportedAtUtc { get; set; }
    public PerformanceAnalysisRequest Request { get; set; } = new();
    public PerformanceAnalysisResult Analysis { get; set; } = new();
    public PerformanceDashboardData Dashboard { get; set; } = new();
}
