namespace PerformanceInsight.Api.Models;

public class PerformanceSample
{
    public string Endpoint { get; set; } = "";
    public double TimeMs { get; set; }
    public bool Success { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string ResponseCode { get; set; } = "";
    public string ResponseMessage { get; set; } = "";
    public string ThreadName { get; set; } = "";
}
