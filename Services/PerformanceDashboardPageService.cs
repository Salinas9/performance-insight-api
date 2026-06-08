using System.Globalization;
using System.Net;
using System.Text;
using PerformanceInsight.Api.Models;

namespace PerformanceInsight.Api.Services;

public class PerformanceDashboardPageService
{
    public string Render(PerformanceDashboardData dashboard)
    {
        var generatedAt = dashboard.GeneratedAtUtc.ToUniversalTime().ToString("dd 'de' MMMM 'de' yyyy, HH:mm 'UTC'");
        var sourceSummary = $"{dashboard.SourceType} · {dashboard.SourceName}";
        var endpointChart = BuildBarChartSvg(dashboard.EndpointAverages);
        var timelineChart = BuildTimelineSvg(dashboard.Timeline);
        var recommendations = string.Join(Environment.NewLine, dashboard.Recommendations.Select((item, index) => $"""
            <li>
              <span class="rec-badge">{index + 1}</span>
              <span>{Encode(item)}</span>
            </li>
            """));
        var issues = string.Join(Environment.NewLine, dashboard.Issues.Select(issue => $"""
            <li>
              <span class="issue-pill {GetIssueCssClass(issue.Severity)}">{Encode(issue.Severity)}</span>
              <div>
                <strong>{Encode(issue.Title)}</strong>
                <p>{Encode(issue.Detail)}</p>
              </div>
            </li>
            """));

        return $$"""
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Performance Insight API</title>
  <style>
    :root {
      --bg: #f4f8ff;
      --surface: rgba(255, 255, 255, 0.92);
      --surface-border: rgba(16, 24, 40, 0.08);
      --text: #14233f;
      --muted: #58709b;
      --navy: #041b3d;
      --navy-2: #0f3d79;
      --blue: #1d69ff;
      --violet: #7b38ed;
      --red: #ff4438;
      --green: #18a957;
      --amber: #ff9f0a;
      --teal: #10a3c7;
      --shadow: 0 18px 40px rgba(9, 30, 66, 0.10);
      --radius-xl: 26px;
      --radius-lg: 20px;
    }

    * { box-sizing: border-box; }
    body {
      margin: 0;
      font-family: "Segoe UI", "Inter", sans-serif;
      background:
        radial-gradient(circle at top left, rgba(29, 105, 255, 0.16), transparent 22%),
        radial-gradient(circle at bottom right, rgba(123, 56, 237, 0.14), transparent 24%),
        linear-gradient(180deg, #eef5ff 0%, #f7faff 100%);
      color: var(--text);
    }

    .page {
      max-width: 1480px;
      margin: 10px auto;
      background: rgba(255, 255, 255, 0.78);
      border: 1px solid rgba(15, 61, 121, 0.14);
      border-radius: 24px;
      box-shadow: var(--shadow);
      overflow: hidden;
      backdrop-filter: blur(10px);
    }

    .hero {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 24px;
      padding: 28px 34px;
      color: white;
      background:
        radial-gradient(circle at 15% 10%, rgba(50, 167, 255, 0.22), transparent 18%),
        linear-gradient(90deg, #04162f 0%, #041b3d 55%, #062347 100%);
    }

    .hero-main {
      display: flex;
      align-items: center;
      gap: 22px;
    }

    .hero h1 {
      margin: 0;
      font-size: clamp(2rem, 4vw, 3.25rem);
      font-weight: 800;
      letter-spacing: -0.03em;
    }

    .hero p {
      margin: 8px 0 0;
      color: #51b8ff;
      font-size: 1.1rem;
      font-weight: 600;
    }

    .hero-date {
      display: flex;
      align-items: center;
      gap: 12px;
      font-size: 1rem;
      color: rgba(255, 255, 255, 0.9);
      white-space: nowrap;
    }

    .hero-side {
      display: grid;
      justify-items: end;
      gap: 8px;
    }

    .source-badge {
      padding: 8px 14px;
      border-radius: 999px;
      background: rgba(81, 184, 255, 0.14);
      border: 1px solid rgba(81, 184, 255, 0.26);
      color: #dff3ff;
      font-size: 0.92rem;
      font-weight: 600;
    }

    .icon-wrap {
      width: 82px;
      height: 82px;
      flex: none;
    }

    .content {
      padding: 32px;
      display: grid;
      gap: 22px;
    }

    .grid-cards {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 18px;
    }

    .grid-panels {
      display: grid;
      grid-template-columns: 1fr 1.1fr;
      gap: 18px;
    }

    .grid-status {
      display: grid;
      grid-template-columns: 1fr 1.25fr;
      gap: 18px;
    }

    .grid-recommendations {
      display: grid;
      grid-template-columns: 1fr;
      gap: 18px;
    }

    .card, .panel {
      background: linear-gradient(180deg, rgba(255,255,255,0.96), rgba(250,252,255,0.96));
      border: 1px solid var(--surface-border);
      border-radius: var(--radius-lg);
      box-shadow: 0 10px 24px rgba(15, 61, 121, 0.06);
    }

    .card {
      padding: 22px 24px;
      display: flex;
      align-items: center;
      gap: 18px;
      min-height: 128px;
      position: relative;
      overflow: hidden;
    }

    .card::before {
      content: "";
      position: absolute;
      inset: 0;
      background: radial-gradient(circle at top left, rgba(255,255,255,0.75), transparent 45%);
      pointer-events: none;
    }

    .card.blue { border-color: rgba(29, 105, 255, 0.35); }
    .card.violet { border-color: rgba(123, 56, 237, 0.35); }
    .card.red { border-color: rgba(255, 68, 56, 0.35); }
    .card.green { border-color: rgba(24, 169, 87, 0.35); }
    .card.amber { border-color: rgba(255, 159, 10, 0.35); }
    .card.teal { border-color: rgba(16, 163, 199, 0.35); }

    .metric-icon {
      width: 88px;
      height: 88px;
      flex: none;
    }

    .metric-title {
      margin: 0 0 6px;
      font-size: 0.95rem;
      color: #3a4b6a;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }

    .metric-label {
      margin: 0 0 8px;
      font-size: 1.05rem;
      font-weight: 700;
      color: #22324d;
    }

    .metric-value {
      margin: 0;
      font-size: clamp(2rem, 3.2vw, 3.7rem);
      font-weight: 800;
      letter-spacing: -0.04em;
    }

    .metric-value small {
      font-size: 0.52em;
      font-weight: 700;
    }

    .blue .metric-value { color: var(--blue); }
    .violet .metric-value { color: var(--violet); }
    .red .metric-value { color: var(--red); }
    .green .metric-value { color: var(--green); }
    .amber .metric-value { color: var(--amber); }
    .teal .metric-value { color: var(--teal); font-size: clamp(1.5rem, 2.5vw, 2.6rem); }

    .panel {
      padding: 20px 22px 18px;
    }

    .panel-head {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 12px;
      margin-bottom: 14px;
    }

    .panel-title {
      margin: 0;
      font-size: 1.05rem;
      font-weight: 800;
      color: #1d2f4d;
    }

    .panel-meta {
      color: var(--muted);
      font-size: 0.95rem;
      font-weight: 600;
    }

    .legend {
      display: flex;
      gap: 18px;
      align-items: center;
      font-size: 0.95rem;
      color: #344561;
      font-weight: 600;
    }

    .legend span {
      display: inline-flex;
      align-items: center;
      gap: 8px;
    }

    .legend i {
      display: inline-block;
      width: 20px;
      height: 4px;
      border-radius: 999px;
    }

    .status-card {
      padding: 26px;
      display: flex;
      gap: 20px;
      align-items: center;
      border: 1px solid rgba(255, 159, 10, 0.28);
      background: linear-gradient(180deg, rgba(255,255,255,0.98), rgba(255,248,236,0.96));
    }

    .status-copy h2 {
      margin: 0 0 6px;
      font-size: 1.1rem;
      color: #304057;
    }

    .status-copy strong {
      display: block;
      font-size: clamp(2rem, 4vw, 3.8rem);
      color: {{GetStatusColor(dashboard.Status)}};
      line-height: 1;
      margin-bottom: 12px;
      letter-spacing: -0.04em;
    }

    .status-copy p {
      margin: 0;
      color: #485a77;
      font-size: 1.02rem;
      line-height: 1.55;
      max-width: 46ch;
    }

    .recommendations {
      padding: 24px 26px;
      border: 1px solid rgba(29, 105, 255, 0.22);
      background: linear-gradient(180deg, rgba(255,255,255,0.98), rgba(242,247,255,0.96));
    }

    .issues {
      padding: 24px 26px;
      border: 1px solid rgba(255, 159, 10, 0.22);
      background: linear-gradient(180deg, rgba(255,255,255,0.98), rgba(255,249,239,0.96));
    }

    .recommendations ul {
      list-style: none;
      padding: 0;
      margin: 18px 0 0;
      display: grid;
      gap: 12px;
    }

    .issues ul {
      list-style: none;
      padding: 0;
      margin: 18px 0 0;
      display: grid;
      gap: 14px;
    }

    .recommendations li {
      display: flex;
      align-items: center;
      gap: 14px;
      padding: 10px 0;
      border-bottom: 1px solid rgba(15, 61, 121, 0.08);
      color: #33445f;
      font-size: 1rem;
    }

    .issues li {
      display: flex;
      gap: 14px;
      padding: 10px 0;
      border-bottom: 1px solid rgba(15, 61, 121, 0.08);
    }

    .recommendations li:last-child {
      border-bottom: 0;
      padding-bottom: 0;
    }

    .issues li:last-child {
      border-bottom: 0;
      padding-bottom: 0;
    }

    .issues li strong {
      display: block;
      color: #20314e;
      margin-bottom: 4px;
    }

    .issues li p {
      margin: 0;
      color: #50637f;
      line-height: 1.45;
      font-size: 0.96rem;
    }

    .rec-badge {
      width: 28px;
      height: 28px;
      border-radius: 999px;
      display: inline-grid;
      place-items: center;
      background: var(--blue);
      color: white;
      font-weight: 700;
      font-size: 0.9rem;
      flex: none;
    }

    .issue-pill {
      min-width: 74px;
      height: 28px;
      padding: 0 10px;
      border-radius: 999px;
      display: inline-grid;
      place-items: center;
      font-weight: 700;
      font-size: 0.84rem;
      flex: none;
      color: white;
    }

    .issue-pill.ok { background: #18a957; }
    .issue-pill.warning { background: #ff9f0a; }
    .issue-pill.critical { background: #ff4438; }

    .source-strip {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 18px;
    }

    .source-item {
      padding: 18px 20px;
      border-radius: 18px;
      border: 1px solid rgba(15, 61, 121, 0.12);
      background: rgba(255, 255, 255, 0.72);
    }

    .source-item span {
      display: block;
      font-size: 0.86rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #657999;
      font-weight: 700;
      margin-bottom: 8px;
    }

    .source-item strong {
      font-size: 1.2rem;
      color: #1e3150;
    }

    footer {
      padding: 0 32px 28px;
      color: #6f83a7;
      display: flex;
      justify-content: center;
      gap: 16px;
      font-size: 0.95rem;
    }

    .chart {
      width: 100%;
      height: auto;
      display: block;
    }

    @media (max-width: 1100px) {
      .grid-cards,
      .grid-panels,
      .grid-status,
      .source-strip {
        grid-template-columns: 1fr;
      }

      .hero {
        flex-direction: column;
        align-items: flex-start;
      }

      .hero-date {
        white-space: normal;
      }

      .hero-side {
        justify-items: start;
      }
    }

    @media (max-width: 720px) {
      .page {
        margin: 0;
        border-radius: 0;
      }

      .content,
      footer {
        padding-left: 18px;
        padding-right: 18px;
      }

      .hero {
        padding: 22px 18px;
      }

      .card,
      .status-card,
      .recommendations {
        padding: 18px;
      }
    }
  </style>
</head>
<body>
  <main class="page">
    <section class="hero">
      <div class="hero-main">
        <div class="icon-wrap">{{BuildHeroIcon()}}</div>
        <div>
          <h1>Performance Insight API</h1>
          <p>Resumen de rendimiento</p>
        </div>
      </div>
      <div class="hero-side">
        <div class="hero-date">
          {{BuildCalendarIcon()}}
          <span>{{generatedAt}}</span>
        </div>
        <div class="source-badge">{{Encode(sourceSummary)}}</div>
      </div>
    </section>

    <section class="content">
      <div class="source-strip">
        <article class="source-item">
          <span>Muestras</span>
          <strong>{{dashboard.TotalSamples}}</strong>
        </article>
        <article class="source-item">
          <span>Endpoints</span>
          <strong>{{dashboard.DistinctEndpoints}}</strong>
        </article>
        <article class="source-item">
          <span>Errores detectados</span>
          <strong>{{dashboard.ErrorCount}}</strong>
        </article>
      </div>

      <div class="grid-cards">
        <article class="card blue">
          {{BuildMetricIcon("clock", "#1d69ff")}}
          <div>
            <p class="metric-title">Latencia media</p>
            <p class="metric-value">{{FormatMetric(dashboard.AverageMs)}} <small>ms</small></p>
          </div>
        </article>

        <article class="card violet">
          {{BuildMetricIcon("gauge", "#7b38ed")}}
          <div>
            <p class="metric-title">P95</p>
            <p class="metric-value">{{FormatMetric(dashboard.P95Ms)}} <small>ms</small></p>
          </div>
        </article>

        <article class="card red">
          {{BuildMetricIcon("trend", "#ff4438")}}
          <div>
            <p class="metric-title">Maxima</p>
            <p class="metric-value">{{FormatMetric(dashboard.MaxMs)}} <small>ms</small></p>
          </div>
        </article>

        <article class="card green">
          {{BuildMetricIcon("throughput", "#18a957")}}
          <div>
            <p class="metric-title">Throughput</p>
            <p class="metric-value">{{FormatMetric(dashboard.ThroughputPerSecond)}} <small>req/s</small></p>
          </div>
        </article>

        <article class="card amber">
          {{BuildMetricIcon("warning", "#ff9f0a")}}
          <div>
            <p class="metric-title">Errores</p>
            <p class="metric-value">{{FormatMetric(dashboard.ErrorRate)}} <small>%</small></p>
          </div>
        </article>

        <article class="card teal">
          {{BuildMetricIcon("endpoint", "#10a3c7")}}
          <div>
            <p class="metric-title">Endpoint mas lento</p>
            <p class="metric-value">{{Encode(dashboard.SlowestEndpoint)}}</p>
          </div>
        </article>
      </div>

      <div class="grid-panels">
        <section class="panel">
          <div class="panel-head">
            <h2 class="panel-title">Tiempo de respuesta promedio por endpoint</h2>
            <span class="panel-meta">ms</span>
          </div>
          {{endpointChart}}
        </section>

        <section class="panel">
          <div class="panel-head">
            <h2 class="panel-title">Latencia (ms) a lo largo del tiempo</h2>
            <div class="legend">
              <span><i style="background:#1d69ff"></i>P50</span>
              <span><i style="background:#7b38ed"></i>P95</span>
              <span><i style="background:#ff4438"></i>Max</span>
            </div>
          </div>
          {{timelineChart}}
        </section>
      </div>

      <div class="grid-status">
        <section class="panel status-card">
          {{BuildMetricIcon("warning", GetStatusColor(dashboard.Status))}}
          <div class="status-copy">
            <h2>Estado general</h2>
            <strong>{{Encode(dashboard.Status)}}</strong>
            <p>{{Encode(dashboard.StatusMessage)}}</p>
          </div>
        </section>

        <section class="panel issues">
          <div class="panel-head">
            <h2 class="panel-title">Problemas detectados</h2>
            <span class="panel-meta">Focos de riesgo</span>
          </div>
          <ul>
            {{issues}}
          </ul>
        </section>
      </div>

      <div class="grid-recommendations">
        <section class="panel recommendations">
          <div class="panel-head">
            <h2 class="panel-title">Recomendaciones</h2>
            <span class="panel-meta">Acciones sugeridas</span>
          </div>
          <ul>
            {{recommendations}}
          </ul>
        </section>
      </div>
    </section>

    <footer>
      <span>Performance Insight API v1.0</span>
      <span>&bull;</span>
      <span>{{Encode(dashboard.SourceType)}}</span>
    </footer>
  </main>
</body>
</html>
""";
    }

    private static string BuildBarChartSvg(IReadOnlyList<EndpointLatencyPoint> points)
    {
        if (points.Count == 0)
        {
            return """<svg class="chart" viewBox="0 0 640 320" xmlns="http://www.w3.org/2000/svg"></svg>""";
        }

        const int width = 640;
        const int height = 320;
        const int left = 64;
        const int right = 18;
        const int top = 22;
        const int bottom = 54;
        var plotWidth = width - left - right;
        var plotHeight = height - top - bottom;
        var maxValue = Math.Max(points.Max(point => point.AverageMs), 100);
        var step = plotWidth / Math.Max(points.Count, 1);
        var barWidth = Math.Min(72, step * 0.55);

        var sb = new StringBuilder();
        sb.Append("""<svg class="chart" viewBox="0 0 640 320" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="Grafico de barras de latencia media por endpoint">""");

        for (var i = 0; i <= 5; i++)
        {
            var value = maxValue * i / 5;
            var y = top + plotHeight - (plotHeight * i / 5.0);
            sb.Append($"""<line x1="{Svg(left)}" y1="{Svg(y)}" x2="{Svg(width - right)}" y2="{Svg(y)}" stroke="rgba(15,61,121,0.14)" stroke-dasharray="4 6" />""");
            sb.Append($"""<text x="{Svg(left - 12)}" y="{Svg(y + 5)}" text-anchor="end" font-size="12" fill="#5b6f92">{Math.Round(value)}</text>""");
        }

        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];
            var x = left + step * i + (step - barWidth) / 2.0;
            var barHeight = point.AverageMs / maxValue * plotHeight;
            var y = top + plotHeight - barHeight;
            var labelX = left + step * i + step / 2.0;

            sb.Append($"""<rect x="{Svg(x)}" y="{Svg(y)}" width="{Svg(barWidth)}" height="{Svg(barHeight)}" rx="10" fill="url(#barGradient)" />""");
            sb.Append($"""<text x="{Svg(labelX)}" y="{Svg(y - 8)}" text-anchor="middle" font-size="13" font-weight="700" fill="#233a67">{Math.Round(point.AverageMs)} ms</text>""");
            sb.Append($"""<text x="{Svg(labelX)}" y="{Svg(height - 18)}" text-anchor="middle" font-size="12" fill="#425572">{Encode(point.Endpoint)}</text>""");
        }

        sb.Append("""
  <defs>
    <linearGradient id="barGradient" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#2276ff"/>
      <stop offset="100%" stop-color="#0b53d9"/>
    </linearGradient>
  </defs>
</svg>
""");

        return sb.ToString();
    }

    private static string BuildTimelineSvg(IReadOnlyList<LatencyTimelinePoint> points)
    {
        if (points.Count == 0)
        {
            return """<svg class="chart" viewBox="0 0 760 320" xmlns="http://www.w3.org/2000/svg"></svg>""";
        }

        const int width = 760;
        const int height = 320;
        const int left = 48;
        const int right = 16;
        const int top = 18;
        const int bottom = 42;
        var plotWidth = width - left - right;
        var plotHeight = height - top - bottom;
        var maxValue = Math.Max(points.Max(point => point.MaxMs), 100);
        var stepX = points.Count == 1 ? 0 : plotWidth / (points.Count - 1.0);

        var sb = new StringBuilder();
        sb.Append("""<svg class="chart" viewBox="0 0 760 320" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="Grafico temporal de latencia p50, p95 y maxima">""");

        for (var i = 0; i <= 5; i++)
        {
            var value = maxValue * i / 5;
            var y = top + plotHeight - (plotHeight * i / 5.0);
            sb.Append($"""<line x1="{Svg(left)}" y1="{Svg(y)}" x2="{Svg(width - right)}" y2="{Svg(y)}" stroke="rgba(15,61,121,0.14)" stroke-dasharray="4 6" />""");
            sb.Append($"""<text x="{Svg(left - 10)}" y="{Svg(y + 5)}" text-anchor="end" font-size="12" fill="#5b6f92">{Math.Round(value)}</text>""");
        }

        var labelStep = Math.Max(1, points.Count / 6);
        for (var i = 0; i < points.Count; i += labelStep)
        {
            var x = left + stepX * i;
            sb.Append($"""<text x="{Svg(x)}" y="{Svg(height - 16)}" text-anchor="middle" font-size="12" fill="#425572">{Encode(points[i].Label)}</text>""");
        }

        sb.Append(BuildPolyline(points, point => point.P50Ms, "#1d69ff", left, top, plotWidth, plotHeight, maxValue));
        sb.Append(BuildPolyline(points, point => point.P95Ms, "#7b38ed", left, top, plotWidth, plotHeight, maxValue));
        sb.Append(BuildPolyline(points, point => point.MaxMs, "#ff4438", left, top, plotWidth, plotHeight, maxValue));
        sb.Append("</svg>");

        return sb.ToString();
    }

    private static string BuildPolyline(
        IReadOnlyList<LatencyTimelinePoint> points,
        Func<LatencyTimelinePoint, double> selector,
        string color,
        int left,
        int top,
        double plotWidth,
        double plotHeight,
        double maxValue)
    {
        var coordinates = points.Select((point, index) =>
        {
            var x = left + (points.Count == 1 ? 0 : plotWidth * index / (points.Count - 1.0));
            var y = top + plotHeight - selector(point) / maxValue * plotHeight;
            return $"{Svg(x)},{Svg(y)}";
        });

        return $"""<polyline fill="none" stroke="{color}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" points="{string.Join(" ", coordinates)}" />""";
    }

    private static string BuildHeroIcon()
    {
        return """
<svg viewBox="0 0 96 96" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="heroStroke" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0%" stop-color="#25b7ff"/>
      <stop offset="100%" stop-color="#1d69ff"/>
    </linearGradient>
  </defs>
  <path d="M48 6 79 24v36L48 90 17 60V24L48 6Z" fill="rgba(37,183,255,0.08)" stroke="url(#heroStroke)" stroke-width="4"/>
  <circle cx="48" cy="44" r="18" fill="none" stroke="url(#heroStroke)" stroke-width="3" stroke-dasharray="3 6"/>
  <path d="M48 44 59 36" stroke="#25b7ff" stroke-width="4" stroke-linecap="round"/>
  <circle cx="48" cy="44" r="4" fill="#25b7ff"/>
  <path d="m34 62 8-8m12 0 8 8m-21 3 7 3 7-3" stroke="#51b8ff" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round" fill="none"/>
</svg>
""";
    }

    private static string BuildCalendarIcon()
    {
        return """
<svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
  <path d="M8 2V6M16 2V6M3 9H21M5 4H19C20.1046 4 21 4.89543 21 6V19C21 20.1046 20.1046 21 19 21H5C3.89543 21 3 20.1046 3 19V6C3 4.89543 3.89543 4 5 4Z" stroke="white" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
</svg>
""";
    }

    private static string BuildMetricIcon(string type, string color)
    {
        return type switch
        {
            "clock" => BuildIconSvg(color, """
<circle cx="48" cy="48" r="33" fill="none" stroke="{0}" stroke-width="3.2"/>
<path d="M48 28V48H35" stroke="{0}" stroke-width="3.6" stroke-linecap="round" stroke-linejoin="round"/>
<circle cx="48" cy="48" r="3.5" fill="{0}"/>
"""),
            "gauge" => BuildIconSvg(color, """
<path d="M18 54a30 30 0 1 1 60 0" fill="none" stroke="{0}" stroke-width="3.2" stroke-linecap="round"/>
<path d="M48 48 62 40" stroke="{0}" stroke-width="3.6" stroke-linecap="round"/>
<circle cx="48" cy="48" r="4" fill="{0}"/>
<path d="M28 33h0M37 25h0M48 22h0M60 25h0" stroke="{0}" stroke-width="5.5" stroke-linecap="round"/>
"""),
            "trend" => BuildIconSvg(color, """
<path d="M18 62 31 28l13 17 16-24 18 12" fill="none" stroke="{0}" stroke-width="3.4" stroke-linecap="round" stroke-linejoin="round"/>
<circle cx="31" cy="28" r="3.4" fill="white" stroke="{0}" stroke-width="2.4"/>
<circle cx="44" cy="45" r="3.4" fill="white" stroke="{0}" stroke-width="2.4"/>
<circle cx="60" cy="21" r="3.4" fill="white" stroke="{0}" stroke-width="2.4"/>
"""),
            "throughput" => BuildIconSvg(color, """
<path d="M18 62V24M18 62H63" fill="none" stroke="{0}" stroke-width="3.2" stroke-linecap="round"/>
<path d="m28 52 11-16 11 9 18-22" fill="none" stroke="{0}" stroke-width="3.4" stroke-linecap="round" stroke-linejoin="round"/>
<path d="M60 23h8v8" fill="none" stroke="{0}" stroke-width="3.2" stroke-linecap="round" stroke-linejoin="round"/>
<circle cx="39" cy="36" r="3" fill="{0}"/>
<circle cx="50" cy="45" r="3" fill="{0}"/>
"""),
            "warning" => BuildIconSvg(color, """
<path d="M48 17 76 68a5 5 0 0 1-4.4 7H24.4A5 5 0 0 1 20 68L48 17Z" fill="none" stroke="{0}" stroke-width="3.2" stroke-linejoin="round"/>
<path d="M48 35V52" stroke="{0}" stroke-width="4.2" stroke-linecap="round"/>
<circle cx="48" cy="62" r="3.5" fill="{0}"/>
"""),
            "endpoint" => BuildIconSvg(color, """
<rect x="18" y="18" width="46" height="18" rx="3.5" fill="none" stroke="{0}" stroke-width="3"/>
<rect x="18" y="40" width="46" height="18" rx="3.5" fill="none" stroke="{0}" stroke-width="3"/>
<rect x="18" y="62" width="46" height="14" rx="3.5" fill="none" stroke="{0}" stroke-width="3"/>
<circle cx="28" cy="27" r="2.7" fill="{0}"/>
<circle cx="28" cy="49" r="2.7" fill="{0}"/>
<circle cx="28" cy="69" r="2.7" fill="{0}"/>
"""),
            _ => BuildIconSvg(color, """<circle cx="48" cy="48" r="32" fill="none" stroke="{0}" stroke-width="3"/>""")
        };
    }

    private static string BuildIconSvg(string color, string bodyTemplate)
    {
        var body = string.Format(bodyTemplate, color);
        return $"""<svg class="metric-icon" viewBox="0 0 96 96" xmlns="http://www.w3.org/2000/svg">{body}</svg>""";
    }

    private static string FormatMetric(double value)
    {
        return value >= 100
            ? Math.Round(value).ToString("0")
            : value.ToString("0.0");
    }

    private static string GetStatusColor(string status)
    {
        return status switch
        {
            "Critical" => "#ff4438",
            "Warning" => "#ff9f0a",
            _ => "#18a957"
        };
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static string GetIssueCssClass(string severity)
    {
        return severity switch
        {
            "Critical" => "critical",
            "Warning" => "warning",
            _ => "ok"
        };
    }

    private static string Svg(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
