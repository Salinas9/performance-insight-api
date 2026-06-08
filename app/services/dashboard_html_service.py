from __future__ import annotations

from html import escape

from app.models.dashboard_data import (
    EndpointLatencyPoint,
    LatencyTimelinePoint,
    PerformanceDashboardData,
)


class DashboardHtmlService:
    def render(self, dashboard: PerformanceDashboardData) -> str:
        generated_at = dashboard.generated_at_utc.strftime("%d/%m/%Y %H:%M UTC")
        source_summary = f"{dashboard.source_type} · {dashboard.source_name}"
        endpoint_chart = self._build_bar_chart_svg(dashboard.endpoint_averages)
        timeline_chart = self._build_timeline_svg(dashboard.timeline)
        issues = "\n".join(
            f"""
            <li class="issue-item">
              <span class="issue-badge {self._issue_css_class(issue.severity)}">{escape(issue.severity)}</span>
              <div>
                <strong>{escape(issue.title)}</strong>
                <p>{escape(issue.detail)}</p>
              </div>
            </li>
            """
            for issue in dashboard.issues
        )
        recommendations = "\n".join(
            f"""
            <li>
              <span class="rec-index">{index}</span>
              <span>{escape(item)}</span>
            </li>
            """
            for index, item in enumerate(dashboard.recommendations, start=1)
        )

        return f"""<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Performance Insight API</title>
  <style>
    :root {{
      --bg: #eef5ff;
      --panel: rgba(255,255,255,0.94);
      --border: rgba(15,61,121,0.14);
      --text: #173257;
      --muted: #617a9f;
      --blue: #1d69ff;
      --violet: #7b38ed;
      --red: #ff4438;
      --green: #18a957;
      --amber: #ff9f0a;
      --teal: #10a3c7;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      font-family: "Segoe UI", Arial, sans-serif;
      color: var(--text);
      background:
        radial-gradient(circle at top left, rgba(29,105,255,0.12), transparent 24%),
        radial-gradient(circle at bottom right, rgba(123,56,237,0.10), transparent 20%),
        linear-gradient(180deg, #eef5ff 0%, #f8fbff 100%);
    }}
    .page {{
      max-width: 1420px;
      margin: 10px auto;
      background: rgba(255,255,255,0.8);
      border: 1px solid rgba(15,61,121,0.12);
      border-radius: 26px;
      box-shadow: 0 18px 45px rgba(9,30,66,0.10);
      overflow: hidden;
    }}
    .hero {{
      display: flex;
      justify-content: space-between;
      gap: 24px;
      padding: 28px 34px;
      color: white;
      background: linear-gradient(90deg, #04162f 0%, #041b3d 55%, #082b56 100%);
    }}
    .hero h1 {{
      margin: 0;
      font-size: clamp(2rem, 4vw, 3rem);
      letter-spacing: -0.04em;
    }}
    .hero p {{ margin: 10px 0 0; color: #61c2ff; font-weight: 700; }}
    .hero-meta {{
      text-align: right;
      align-self: center;
      display: grid;
      gap: 8px;
      font-size: 0.98rem;
    }}
    .source-chip {{
      border: 1px solid rgba(97,194,255,0.25);
      background: rgba(97,194,255,0.12);
      color: #e8f7ff;
      border-radius: 999px;
      padding: 8px 14px;
      font-weight: 600;
    }}
    .content {{
      padding: 28px;
      display: grid;
      gap: 18px;
    }}
    .strip, .cards, .charts, .bottom {{
      display: grid;
      gap: 18px;
    }}
    .strip {{ grid-template-columns: repeat(3, minmax(0, 1fr)); }}
    .cards {{ grid-template-columns: repeat(3, minmax(0, 1fr)); }}
    .charts {{ grid-template-columns: 1fr 1.1fr; }}
    .bottom {{ grid-template-columns: 1fr 1.2fr; }}
    .box, .panel {{
      background: var(--panel);
      border: 1px solid var(--border);
      border-radius: 22px;
      box-shadow: 0 10px 25px rgba(9,30,66,0.06);
    }}
    .box {{
      padding: 18px 20px;
    }}
    .box span {{
      display: block;
      text-transform: uppercase;
      color: var(--muted);
      font-size: 0.82rem;
      font-weight: 700;
      letter-spacing: 0.05em;
      margin-bottom: 8px;
    }}
    .box strong {{ font-size: 1.3rem; }}
    .card {{
      display: flex;
      align-items: center;
      gap: 16px;
      min-height: 120px;
      padding: 20px;
    }}
    .card.blue {{ border-color: rgba(29,105,255,0.30); }}
    .card.violet {{ border-color: rgba(123,56,237,0.30); }}
    .card.red {{ border-color: rgba(255,68,56,0.30); }}
    .card.green {{ border-color: rgba(24,169,87,0.30); }}
    .card.amber {{ border-color: rgba(255,159,10,0.30); }}
    .card.teal {{ border-color: rgba(16,163,199,0.30); }}
    .icon {{
      width: 76px;
      height: 76px;
      flex: none;
      display: grid;
      place-items: center;
      border-radius: 18px;
      background: linear-gradient(180deg, rgba(255,255,255,0.95), rgba(246,250,255,0.92));
      border: 1px solid rgba(15,61,121,0.08);
      font-size: 2rem;
    }}
    .card h2 {{
      margin: 0 0 6px;
      font-size: 1rem;
      color: #314867;
    }}
    .value {{
      margin: 0;
      font-size: clamp(1.8rem, 3vw, 3.4rem);
      font-weight: 800;
      letter-spacing: -0.04em;
    }}
    .value small {{ font-size: 0.5em; }}
    .blue .value {{ color: var(--blue); }}
    .violet .value {{ color: var(--violet); }}
    .red .value {{ color: var(--red); }}
    .green .value {{ color: var(--green); }}
    .amber .value {{ color: var(--amber); }}
    .teal .value {{ color: var(--teal); font-size: clamp(1.2rem, 2vw, 2.2rem); }}
    .panel {{
      padding: 20px 22px;
    }}
    .panel-head {{
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 12px;
      margin-bottom: 12px;
    }}
    .panel-head h3 {{
      margin: 0;
      font-size: 1.05rem;
    }}
    .panel-head span {{
      color: var(--muted);
      font-weight: 600;
    }}
    .status {{
      display: flex;
      gap: 18px;
      align-items: center;
      border-color: rgba(255,159,10,0.25);
      background: linear-gradient(180deg, rgba(255,255,255,0.98), rgba(255,249,239,0.96));
    }}
    .status strong {{
      display: block;
      font-size: clamp(2rem, 4vw, 3.4rem);
      line-height: 1;
      margin-bottom: 10px;
      color: {self._status_color(dashboard.status)};
    }}
    .status p {{
      margin: 0;
      color: #4c607d;
      line-height: 1.5;
    }}
    .issue-list, .rec-list {{
      list-style: none;
      padding: 0;
      margin: 16px 0 0;
      display: grid;
      gap: 12px;
    }}
    .issue-item, .rec-list li {{
      display: flex;
      gap: 12px;
      align-items: flex-start;
      padding-bottom: 10px;
      border-bottom: 1px solid rgba(15,61,121,0.08);
    }}
    .issue-item:last-child, .rec-list li:last-child {{ border-bottom: 0; padding-bottom: 0; }}
    .issue-badge {{
      min-width: 78px;
      padding: 6px 10px;
      border-radius: 999px;
      color: white;
      font-size: 0.82rem;
      font-weight: 700;
      text-align: center;
    }}
    .issue-badge.ok {{ background: #18a957; }}
    .issue-badge.warning {{ background: #ff9f0a; }}
    .issue-badge.critical {{ background: #ff4438; }}
    .issue-item strong {{ display: block; margin-bottom: 4px; }}
    .issue-item p {{ margin: 0; color: #576b88; line-height: 1.45; }}
    .rec-index {{
      width: 28px;
      height: 28px;
      display: grid;
      place-items: center;
      border-radius: 999px;
      background: var(--blue);
      color: white;
      font-weight: 700;
      flex: none;
    }}
    .chart {{
      width: 100%;
      height: auto;
      display: block;
    }}
    footer {{
      padding: 0 28px 26px;
      text-align: center;
      color: #6c81a2;
    }}
    @media (max-width: 1100px) {{
      .strip, .cards, .charts, .bottom {{ grid-template-columns: 1fr; }}
      .hero {{ flex-direction: column; }}
      .hero-meta {{ text-align: left; }}
    }}
    @media (max-width: 720px) {{
      .page {{ margin: 0; border-radius: 0; }}
      .content, footer {{ padding-left: 16px; padding-right: 16px; }}
      .hero {{ padding: 22px 16px; }}
    }}
  </style>
</head>
<body>
  <main class="page">
    <section class="hero">
      <div>
        <h1>Performance Insight API</h1>
        <p>Resumen de rendimiento</p>
      </div>
      <div class="hero-meta">
        <div>{generated_at}</div>
        <div class="source-chip">{escape(source_summary)}</div>
      </div>
    </section>

    <section class="content">
      <div class="strip">
        <article class="box"><span>Muestras</span><strong>{dashboard.total_samples}</strong></article>
        <article class="box"><span>Endpoints</span><strong>{dashboard.distinct_endpoints}</strong></article>
        <article class="box"><span>Errores detectados</span><strong>{dashboard.error_count}</strong></article>
      </div>

      <div class="cards">
        {self._metric_card("blue", "Latencia media", f"{self._format_metric(dashboard.average_ms)} <small>ms</small>", "⏱")}
        {self._metric_card("violet", "P95", f"{self._format_metric(dashboard.p95_ms)} <small>ms</small>", "📈")}
        {self._metric_card("red", "Máxima", f"{self._format_metric(dashboard.max_ms)} <small>ms</small>", "🔥")}
        {self._metric_card("green", "Throughput", f"{self._format_metric(dashboard.throughput_per_second)} <small>req/s</small>", "⚡")}
        {self._metric_card("amber", "Errores", f"{self._format_metric(dashboard.error_rate)} <small>%</small>", "⚠")}
        {self._metric_card("teal", "Endpoint más lento", escape(dashboard.slowest_endpoint), "🧩")}
      </div>

      <div class="charts">
        <section class="panel">
          <div class="panel-head">
            <h3>Tiempo de respuesta promedio por endpoint</h3>
            <span>ms</span>
          </div>
          {endpoint_chart}
        </section>
        <section class="panel">
          <div class="panel-head">
            <h3>Latencia a lo largo del tiempo</h3>
            <span>P50 · P95 · Máx</span>
          </div>
          {timeline_chart}
        </section>
      </div>

      <div class="bottom">
        <section class="panel status">
          <div class="icon">🎯</div>
          <div>
            <h3>Estado general</h3>
            <strong>{escape(dashboard.status)}</strong>
            <p>{escape(dashboard.status_message)}</p>
          </div>
        </section>
        <section class="panel">
          <div class="panel-head">
            <h3>Problemas detectados</h3>
            <span>Focos de riesgo</span>
          </div>
          <ul class="issue-list">{issues}</ul>
        </section>
      </div>

      <section class="panel">
        <div class="panel-head">
          <h3>Recomendaciones</h3>
          <span>Acciones sugeridas</span>
        </div>
        <ul class="rec-list">{recommendations}</ul>
      </section>
    </section>

    <footer>Performance Insight API en Python + FastAPI · Fuente: {escape(dashboard.source_type)}</footer>
  </main>
</body>
</html>"""

    @staticmethod
    def _metric_card(css_class: str, title: str, value_html: str, icon: str) -> str:
        return f"""
        <article class="panel card {css_class}">
          <div class="icon">{icon}</div>
          <div>
            <h2>{escape(title)}</h2>
            <p class="value">{value_html}</p>
          </div>
        </article>
        """

    def _build_bar_chart_svg(self, points: list[EndpointLatencyPoint]) -> str:
        if not points:
            return '<svg class="chart" viewBox="0 0 640 320" xmlns="http://www.w3.org/2000/svg"></svg>'

        width, height = 640, 320
        left, right, top, bottom = 64, 20, 22, 56
        plot_width = width - left - right
        plot_height = height - top - bottom
        max_value = max(max(point.average_ms for point in points), 100)
        step = plot_width / max(len(points), 1)
        bar_width = min(72, step * 0.55)
        parts = [
            '<svg class="chart" viewBox="0 0 640 320" xmlns="http://www.w3.org/2000/svg" role="img">'
        ]

        for index in range(6):
            value = max_value * index / 5
            y = top + plot_height - (plot_height * index / 5)
            parts.append(
                f'<line x1="{left}" y1="{y:.2f}" x2="{width-right}" y2="{y:.2f}" '
                'stroke="rgba(15,61,121,0.14)" stroke-dasharray="4 6" />'
            )
            parts.append(
                f'<text x="{left-12}" y="{y+5:.2f}" text-anchor="end" font-size="12" fill="#5b6f92">{round(value)}</text>'
            )

        parts.append(
            """
            <defs>
              <linearGradient id="barGradientPy" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stop-color="#2276ff"/>
                <stop offset="100%" stop-color="#0b53d9"/>
              </linearGradient>
            </defs>
            """
        )

        for index, point in enumerate(points):
            x = left + step * index + (step - bar_width) / 2
            bar_height = point.average_ms / max_value * plot_height
            y = top + plot_height - bar_height
            label_x = left + step * index + step / 2
            parts.append(
                f'<rect x="{x:.2f}" y="{y:.2f}" width="{bar_width:.2f}" height="{bar_height:.2f}" '
                'rx="10" fill="url(#barGradientPy)" />'
            )
            parts.append(
                f'<text x="{label_x:.2f}" y="{y-8:.2f}" text-anchor="middle" font-size="13" font-weight="700" fill="#233a67">{round(point.average_ms)} ms</text>'
            )
            parts.append(
                f'<text x="{label_x:.2f}" y="{height-18}" text-anchor="middle" font-size="12" fill="#425572">{escape(point.endpoint)}</text>'
            )

        parts.append("</svg>")
        return "".join(parts)

    def _build_timeline_svg(self, points: list[LatencyTimelinePoint]) -> str:
        if not points:
            return '<svg class="chart" viewBox="0 0 760 320" xmlns="http://www.w3.org/2000/svg"></svg>'

        width, height = 760, 320
        left, right, top, bottom = 48, 16, 18, 42
        plot_width = width - left - right
        plot_height = height - top - bottom
        max_value = max(max(point.max_ms for point in points), 100)
        step_x = 0 if len(points) == 1 else plot_width / (len(points) - 1)
        parts = [
            '<svg class="chart" viewBox="0 0 760 320" xmlns="http://www.w3.org/2000/svg" role="img">'
        ]

        for index in range(6):
            value = max_value * index / 5
            y = top + plot_height - (plot_height * index / 5)
            parts.append(
                f'<line x1="{left}" y1="{y:.2f}" x2="{width-right}" y2="{y:.2f}" '
                'stroke="rgba(15,61,121,0.14)" stroke-dasharray="4 6" />'
            )
            parts.append(
                f'<text x="{left-10}" y="{y+5:.2f}" text-anchor="end" font-size="12" fill="#5b6f92">{round(value)}</text>'
            )

        label_step = max(1, len(points) // 6 or 1)
        for index in range(0, len(points), label_step):
            x = left + step_x * index
            parts.append(
                f'<text x="{x:.2f}" y="{height-16}" text-anchor="middle" font-size="12" fill="#425572">{escape(points[index].label)}</text>'
            )

        parts.append(self._build_polyline(points, "p50_ms", "#1d69ff", left, top, plot_width, plot_height, max_value))
        parts.append(self._build_polyline(points, "p95_ms", "#7b38ed", left, top, plot_width, plot_height, max_value))
        parts.append(self._build_polyline(points, "max_ms", "#ff4438", left, top, plot_width, plot_height, max_value))
        parts.append("</svg>")
        return "".join(parts)

    @staticmethod
    def _build_polyline(
        points: list[LatencyTimelinePoint],
        field_name: str,
        color: str,
        left: int,
        top: int,
        plot_width: float,
        plot_height: float,
        max_value: float,
    ) -> str:
        coordinates: list[str] = []
        for index, point in enumerate(points):
            x = left if len(points) == 1 else left + plot_width * index / (len(points) - 1)
            value = getattr(point, field_name)
            y = top + plot_height - value / max_value * plot_height
            coordinates.append(f"{x:.2f},{y:.2f}")
        return (
            f'<polyline fill="none" stroke="{color}" stroke-width="3" '
            f'stroke-linecap="round" stroke-linejoin="round" points="{" ".join(coordinates)}" />'
        )

    @staticmethod
    def _format_metric(value: float) -> str:
        if value >= 100:
            return f"{round(value):.0f}"
        return f"{value:.1f}"

    @staticmethod
    def _status_color(status: str) -> str:
        return {"Critical": "#ff4438", "Warning": "#ff9f0a"}.get(status, "#18a957")

    @staticmethod
    def _issue_css_class(severity: str) -> str:
        normalized = severity.lower()
        if normalized == "critical":
            return "critical"
        if normalized == "warning":
            return "warning"
        return "ok"


dashboard_html_service = DashboardHtmlService()
