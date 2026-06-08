using PerformanceInsight.Api.Models;

namespace PerformanceInsight.Api.Services;

public class PerformanceAnalysisService
{
    public PerformanceAnalysisResult Analyze(List<PerformanceSample> samples)
    {
        var times = GetOrderedTimes(samples);
        var average = times.Average();
        var min = times.First();
        var max = times.Last();
        var p95 = GetPercentile(times, 0.95);
        var errorRate = GetErrorRate(samples);
        var endpointHealth = GetEndpointHealth(samples);
        var slowestEndpoint = endpointHealth
            .OrderByDescending(endpoint => endpoint.AverageMs)
            .First()
            .Endpoint;
        var (status, recommendations, _) = BuildStatusSummary(average, p95, errorRate, slowestEndpoint, endpointHealth);

        return new PerformanceAnalysisResult
        {
            TotalSamples = samples.Count,
            AverageMs = Math.Round(average, 2),
            MinMs = Math.Round(min, 2),
            MaxMs = Math.Round(max, 2),
            P95Ms = Math.Round(p95, 2),
            ErrorRate = Math.Round(errorRate, 2),
            SlowestEndpoint = slowestEndpoint,
            Status = status,
            Recommendations = recommendations
        };
    }

    public PerformanceDashboardData BuildDashboard(
        List<PerformanceSample> samples,
        string sourceName = "Datos simulados",
        string sourceType = "Demo")
    {
        var times = GetOrderedTimes(samples);
        var average = times.Average();
        var p95 = GetPercentile(times, 0.95);
        var max = times.Last();
        var errorRate = GetErrorRate(samples);
        var endpointAverages = GetEndpointAverages(samples);
        var endpointHealth = GetEndpointHealth(samples);
        var slowestEndpoint = endpointHealth
            .OrderByDescending(endpoint => endpoint.AverageMs)
            .First()
            .Endpoint;
        var throughput = GetThroughputPerSecond(samples);
        var timeline = BuildTimeline(samples);
        var (status, recommendations, statusMessage) = BuildStatusSummary(
            average,
            p95,
            errorRate,
            slowestEndpoint,
            endpointHealth);
        var issues = BuildIssues(endpointHealth);

        return new PerformanceDashboardData
        {
            GeneratedAtUtc = samples.Max(sample => sample.Timestamp).ToUniversalTime(),
            SourceName = sourceName,
            SourceType = sourceType,
            TotalSamples = samples.Count,
            DistinctEndpoints = samples
                .Select(sample => sample.Endpoint)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            ErrorCount = samples.Count(sample => !sample.Success),
            AverageMs = Math.Round(average, 2),
            P95Ms = Math.Round(p95, 2),
            MaxMs = Math.Round(max, 2),
            ThroughputPerSecond = Math.Round(throughput, 2),
            ErrorRate = Math.Round(errorRate, 2),
            SlowestEndpoint = slowestEndpoint,
            Status = status,
            StatusMessage = statusMessage,
            Recommendations = recommendations,
            Issues = issues,
            EndpointAverages = endpointAverages,
            EndpointHealth = endpointHealth,
            Timeline = timeline
        };
    }

    public PerformanceAnalysisRequest CreateDemoRequest()
    {
        var random = new Random(42);
        var start = DateTimeOffset.UtcNow.AddMinutes(-60);
        var endpoints = new[]
        {
            "/api/health",
            "/api/users",
            "/api/orders",
            "/api/network/analyze"
        };

        var samples = new List<PerformanceSample>();

        for (var minute = 0; minute < 60; minute++)
        {
            var bucketTime = start.AddMinutes(minute);

            for (var call = 0; call < endpoints.Length; call++)
            {
                var endpoint = endpoints[call];
                var baseline = endpoint switch
                {
                    "/api/health" => 120,
                    "/api/users" => 210,
                    "/api/orders" => 340,
                    "/api/network/analyze" => 720,
                    _ => 180
                };

                var drift = 20 * Math.Sin(minute / 5.0 + call);
                var jitter = random.Next(-30, 45);
                var spike = endpoint == "/api/network/analyze" && minute % 11 == 0
                    ? random.Next(180, 420)
                    : 0;
                var timeMs = Math.Max(55, baseline + drift + jitter + spike);
                var success = !(endpoint == "/api/network/analyze" && (minute % 14 == 0 || timeMs > 1000));

                samples.Add(new PerformanceSample
                {
                    Endpoint = endpoint,
                    TimeMs = Math.Round(timeMs, 2),
                    Success = success,
                    Timestamp = bucketTime.AddSeconds(call * 12),
                    ResponseCode = success ? "200" : "500",
                    ResponseMessage = success ? "OK" : "Server Error",
                    ThreadName = $"demo-thread-{call + 1}"
                });
            }
        }

        return new PerformanceAnalysisRequest
        {
            Samples = samples
        };
    }

    private static List<double> GetOrderedTimes(IEnumerable<PerformanceSample> samples)
    {
        return samples
            .Select(sample => sample.TimeMs)
            .OrderBy(time => time)
            .ToList();
    }

    private static double GetPercentile(IReadOnlyList<double> orderedValues, double percentile)
    {
        var index = (int)Math.Ceiling(orderedValues.Count * percentile) - 1;
        index = Math.Clamp(index, 0, orderedValues.Count - 1);
        return orderedValues[index];
    }

    private static double GetErrorRate(IReadOnlyCollection<PerformanceSample> samples)
    {
        return samples.Count(sample => !sample.Success) * 100.0 / samples.Count;
    }

    private static double GetThroughputPerSecond(List<PerformanceSample> samples)
    {
        var minTimestamp = samples.Min(sample => sample.Timestamp);
        var maxTimestamp = samples.Max(sample => sample.Timestamp);
        var durationSeconds = Math.Max((maxTimestamp - minTimestamp).TotalSeconds, 1);
        return samples.Count / durationSeconds;
    }

    private static List<EndpointLatencyPoint> GetEndpointAverages(IEnumerable<PerformanceSample> samples)
    {
        return samples
            .GroupBy(sample => sample.Endpoint)
            .Select(group => new EndpointLatencyPoint
            {
                Endpoint = group.Key,
                AverageMs = Math.Round(group.Average(sample => sample.TimeMs), 2)
            })
            .OrderBy(point => point.AverageMs)
            .ToList();
    }

    private static List<LatencyTimelinePoint> BuildTimeline(IEnumerable<PerformanceSample> samples)
    {
        var orderedSamples = samples
            .OrderBy(sample => sample.Timestamp)
            .ToList();

        var minTimestamp = orderedSamples.First().Timestamp.ToUniversalTime();
        var maxTimestamp = orderedSamples.Last().Timestamp.ToUniversalTime();
        var duration = maxTimestamp - minTimestamp;
        var bucketSeconds = duration.TotalMinutes switch
        {
            <= 2 => 5,
            <= 10 => 15,
            <= 60 => 60,
            _ => 300
        };
        var labelFormat = bucketSeconds < 60 ? "HH:mm:ss" : "HH:mm";

        return samples
            .GroupBy(sample => FloorTimestamp(sample.Timestamp.ToUniversalTime(), bucketSeconds))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var orderedTimes = group
                    .Select(sample => sample.TimeMs)
                    .OrderBy(time => time)
                    .ToList();

                return new LatencyTimelinePoint
                {
                    Label = group.Key.ToUniversalTime().ToString(labelFormat),
                    P50Ms = Math.Round(GetPercentile(orderedTimes, 0.50), 2),
                    P95Ms = Math.Round(GetPercentile(orderedTimes, 0.95), 2),
                    MaxMs = Math.Round(orderedTimes.Last(), 2)
                };
            })
            .ToList();
    }

    private static List<EndpointHealthPoint> GetEndpointHealth(IEnumerable<PerformanceSample> samples)
    {
        return samples
            .GroupBy(sample => sample.Endpoint)
            .Select(group =>
            {
                var sampleList = group.ToList();
                var orderedTimes = sampleList
                    .Select(sample => sample.TimeMs)
                    .OrderBy(time => time)
                    .ToList();
                var errorCount = sampleList.Count(sample => !sample.Success);
                var primaryResponseCode = sampleList
                    .Select(sample => sample.ResponseCode)
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .GroupBy(code => code)
                    .OrderByDescending(codeGroup => codeGroup.Count())
                    .ThenBy(codeGroup => codeGroup.Key)
                    .Select(codeGroup => codeGroup.Key)
                    .FirstOrDefault() ?? "";

                return new EndpointHealthPoint
                {
                    Endpoint = group.Key,
                    Samples = sampleList.Count,
                    ErrorCount = errorCount,
                    AverageMs = Math.Round(orderedTimes.Average(), 2),
                    P95Ms = Math.Round(GetPercentile(orderedTimes, 0.95), 2),
                    MaxMs = Math.Round(orderedTimes.Last(), 2),
                    ErrorRate = Math.Round(errorCount * 100.0 / sampleList.Count, 2),
                    PrimaryResponseCode = primaryResponseCode
                };
            })
            .OrderByDescending(point => point.ErrorRate)
            .ThenByDescending(point => point.P95Ms)
            .ThenByDescending(point => point.AverageMs)
            .ToList();
    }

    private static List<PerformanceIssue> BuildIssues(IEnumerable<EndpointHealthPoint> endpointHealth)
    {
        var issues = new List<PerformanceIssue>();

        foreach (var endpoint in endpointHealth)
        {
            if (endpoint.ErrorRate >= 1)
            {
                issues.Add(new PerformanceIssue
                {
                    Severity = endpoint.ErrorRate >= 10 ? "Critical" : "Warning",
                    Title = $"Errores en {endpoint.Endpoint}",
                    Detail = $"{endpoint.ErrorCount} fallos de {endpoint.Samples} muestras ({endpoint.ErrorRate:0.##}%). Codigo dominante: {endpoint.PrimaryResponseCode}.",
                    Endpoint = endpoint.Endpoint
                });
            }

            if (endpoint.P95Ms >= 1000)
            {
                issues.Add(new PerformanceIssue
                {
                    Severity = endpoint.P95Ms >= 2000 ? "Critical" : "Warning",
                    Title = $"Latencia alta en {endpoint.Endpoint}",
                    Detail = $"P95 de {Math.Round(endpoint.P95Ms)} ms, media de {Math.Round(endpoint.AverageMs)} ms y maximo de {Math.Round(endpoint.MaxMs)} ms.",
                    Endpoint = endpoint.Endpoint
                });
            }
        }

        if (issues.Count == 0)
        {
            issues.Add(new PerformanceIssue
            {
                Severity = "OK",
                Title = "Sin incidencias destacadas",
                Detail = "La ultima carga no presenta errores ni picos graves de latencia."
            });
        }

        return issues
            .OrderByDescending(issue => GetSeverityRank(issue.Severity))
            .ThenBy(issue => issue.Title, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
    }

    private static (string Status, List<string> Recommendations, string StatusMessage) BuildStatusSummary(
        double average,
        double p95,
        double errorRate,
        string slowestEndpoint,
        IReadOnlyList<EndpointHealthPoint> endpointHealth)
    {
        var recommendations = new List<string>();
        var status = "OK";
        var statusMessage = "El rendimiento general es estable y no presenta degradaciones relevantes.";
        var mostFailingEndpoint = endpointHealth
            .OrderByDescending(endpoint => endpoint.ErrorRate)
            .ThenByDescending(endpoint => endpoint.ErrorCount)
            .FirstOrDefault(endpoint => endpoint.ErrorCount > 0);

        if (p95 > 1000 || errorRate > 10)
        {
            status = "Critical";
            statusMessage = "El sistema presenta incidencias severas y requiere atencion inmediata.";
            recommendations.Add($"Revisar la latencia extrema del endpoint {slowestEndpoint}.");
            recommendations.Add("Analizar posibles errores de servidor o cuellos de botella.");
            recommendations.Add("Investigar picos de saturacion en el percentil 95.");
        }
        else if (average > 500 || errorRate > 2)
        {
            status = "Warning";
            statusMessage = "El rendimiento del sistema presenta degradaciones moderadas que requieren atencion.";
            recommendations.Add($"Optimizar el endpoint mas lento: {slowestEndpoint}.");
            recommendations.Add("Reducir picos del percentil 95.");
        }
        else
        {
            recommendations.Add("El rendimiento general es estable.");
            recommendations.Add("Mantener la vigilancia de endpoints criticos.");
            recommendations.Add("Seguir agregando muestras historicas.");
        }

        if (mostFailingEndpoint != null)
        {
            recommendations.Add(
                $"Investigar los fallos en {mostFailingEndpoint.Endpoint} ({mostFailingEndpoint.ErrorRate:0.##}% de error).");
        }

        return (status, recommendations, statusMessage);
    }

    private static DateTimeOffset FloorTimestamp(DateTimeOffset timestamp, int bucketSeconds)
    {
        var utc = timestamp.ToUniversalTime();
        var ticksPerBucket = TimeSpan.FromSeconds(bucketSeconds).Ticks;
        var flooredTicks = utc.Ticks - (utc.Ticks % ticksPerBucket);
        return new DateTimeOffset(flooredTicks, TimeSpan.Zero);
    }

    private static int GetSeverityRank(string severity)
    {
        return severity switch
        {
            "Critical" => 3,
            "Warning" => 2,
            "OK" => 1,
            _ => 0
        };
    }
}
