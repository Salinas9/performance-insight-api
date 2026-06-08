namespace PerformanceInsight.Api.Models;

public class PerformanceDashboardData
{
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public string SourceType { get; set; } = "Demo";
    public string SourceName { get; set; } = "Datos simulados";
    public int TotalSamples { get; set; }
    public int DistinctEndpoints { get; set; }
    public int ErrorCount { get; set; }
    public double AverageMs { get; set; }
    public double P95Ms { get; set; }
    public double MaxMs { get; set; }
    public double ThroughputPerSecond { get; set; }
    public double ErrorRate { get; set; }
    public string SlowestEndpoint { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusMessage { get; set; } = "";
    public List<string> Recommendations { get; set; } = new();
    public List<PerformanceIssue> Issues { get; set; } = new();
    public List<EndpointLatencyPoint> EndpointAverages { get; set; } = new();
    public List<EndpointHealthPoint> EndpointHealth { get; set; } = new();
    public List<LatencyTimelinePoint> Timeline { get; set; } = new();
}

public class EndpointLatencyPoint
{
    public string Endpoint { get; set; } = "";
    public double AverageMs { get; set; }
}

public class LatencyTimelinePoint
{
    public string Label { get; set; } = "";
    public double P50Ms { get; set; }
    public double P95Ms { get; set; }
    public double MaxMs { get; set; }
}

public class EndpointHealthPoint
{
    public string Endpoint { get; set; } = "";
    public int Samples { get; set; }
    public int ErrorCount { get; set; }
    public double AverageMs { get; set; }
    public double P95Ms { get; set; }
    public double MaxMs { get; set; }
    public double ErrorRate { get; set; }
    public string PrimaryResponseCode { get; set; } = "";
}

public class PerformanceIssue
{
    public string Severity { get; set; } = "Info";
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Endpoint { get; set; } = "";
}
