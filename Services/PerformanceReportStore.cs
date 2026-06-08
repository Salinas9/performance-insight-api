using PerformanceInsight.Api.Models;

namespace PerformanceInsight.Api.Services;

public class PerformanceReportStore
{
    private readonly object _sync = new();
    private PerformanceReportSnapshot? _latestSnapshot;

    public PerformanceReportSnapshot? GetLatest()
    {
        lock (_sync)
        {
            return _latestSnapshot;
        }
    }

    public void Save(PerformanceReportSnapshot snapshot)
    {
        lock (_sync)
        {
            _latestSnapshot = snapshot;
        }
    }
}
