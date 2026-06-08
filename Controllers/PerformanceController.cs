using Microsoft.AspNetCore.Mvc;
using PerformanceInsight.Api.Models;
using PerformanceInsight.Api.Services;

namespace PerformanceInsight.Api.Controllers;

[ApiController]
[Route("")]
public class PerformanceController : ControllerBase
{
    private readonly PerformanceAnalysisService _service;
    private readonly PerformanceDashboardPageService _dashboardPageService;
    private readonly JMeterResultParserService _jmeterResultParserService;
    private readonly PerformanceReportStore _reportStore;

    public PerformanceController(
        PerformanceAnalysisService service,
        PerformanceDashboardPageService dashboardPageService,
        JMeterResultParserService jmeterResultParserService,
        PerformanceReportStore reportStore)
    {
        _service = service;
        _dashboardPageService = dashboardPageService;
        _jmeterResultParserService = jmeterResultParserService;
        _reportStore = reportStore;
    }

    [HttpGet("")]
    public ContentResult Dashboard()
    {
        var snapshot = GetCurrentSnapshot();
        var dashboard = snapshot.Dashboard;
        var html = _dashboardPageService.Render(dashboard);

        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet("api/performance/health")]
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            service = "Performance Insight API"
        });
    }

    [HttpGet("api/performance/example")]
    [HttpGet("example")]
    public ActionResult<PerformanceAnalysisRequest> Example()
    {
        return Ok(_service.CreateDemoRequest());
    }

    [HttpGet("api/performance/dashboard-data")]
    public ActionResult<PerformanceDashboardData> DashboardData()
    {
        return Ok(GetCurrentSnapshot().Dashboard);
    }

    [HttpGet("api/performance/current-report")]
    public ActionResult<PerformanceReportSnapshot> CurrentReport()
    {
        return Ok(GetCurrentSnapshot());
    }

    [HttpPost("api/performance/dashboard")]
    public ActionResult<PerformanceDashboardData> BuildDashboard(PerformanceAnalysisRequest request)
    {
        if (request.Samples == null || request.Samples.Count == 0)
        {
            return BadRequest("Samples cannot be empty.");
        }

        var snapshot = CreateSnapshot(request.Samples, "Carga manual", "API JSON");
        _reportStore.Save(snapshot);

        return Ok(snapshot.Dashboard);
    }

    [HttpPost("api/performance/jmeter/upload")]
    public async Task<ActionResult<PerformanceReportSnapshot>> UploadJMeterResults(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Debes subir un archivo .jtl o .csv de JMeter.");
        }

        List<PerformanceSample> samples;

        try
        {
            await using var stream = file.OpenReadStream();
            samples = _jmeterResultParserService.ParseCsv(stream);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        if (samples.Count == 0)
        {
            return BadRequest("No se pudieron extraer muestras validas del archivo de JMeter.");
        }

        var snapshot = CreateSnapshot(samples, file.FileName, "JMeter CSV");
        _reportStore.Save(snapshot);

        return Ok(snapshot);
    }

    [HttpPost("api/performance/analyze")]
    [HttpPost("analyze")]
    public ActionResult<PerformanceAnalysisResult> Analyze(PerformanceAnalysisRequest request)
    {
        if (request.Samples == null || request.Samples.Count == 0)
        {
            return BadRequest("Samples cannot be empty.");
        }

        var result = _service.Analyze(request.Samples);

        return Ok(result);
    }

    private PerformanceReportSnapshot GetCurrentSnapshot()
    {
        return _reportStore.GetLatest()
            ?? CreateSnapshot(_service.CreateDemoRequest().Samples, "Datos simulados", "Demo");
    }

    private PerformanceReportSnapshot CreateSnapshot(
        List<PerformanceSample> samples,
        string sourceName,
        string sourceType)
    {
        var request = new PerformanceAnalysisRequest
        {
            Samples = samples
        };
        var analysis = _service.Analyze(samples);
        var dashboard = _service.BuildDashboard(samples, sourceName, sourceType);

        return new PerformanceReportSnapshot
        {
            SourceName = sourceName,
            SourceType = sourceType,
            ImportedAtUtc = DateTimeOffset.UtcNow,
            Request = request,
            Analysis = analysis,
            Dashboard = dashboard
        };
    }
}
