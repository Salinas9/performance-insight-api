from __future__ import annotations

from datetime import datetime, timezone

from fastapi import APIRouter, File, HTTPException, UploadFile
from fastapi.responses import HTMLResponse

from app.models import (
    PerformanceAnalysisRequest,
    PerformanceAnalysisResult,
    PerformanceDashboardData,
    PerformanceReportSnapshot,
    PerformanceSample,
)
from app.services.dashboard_html_service import dashboard_html_service
from app.services.jmeter_result_parser_service import JMeterResultParserService
from app.services.performance_analysis_service import analysis_service
from app.services.report_store import report_store


router = APIRouter()
parser_service = JMeterResultParserService()


@router.get("/", response_class=HTMLResponse)
def dashboard() -> HTMLResponse:
    snapshot = _get_current_snapshot()
    html = dashboard_html_service.render(snapshot.dashboard)
    return HTMLResponse(content=html)


@router.get("/health")
@router.get("/api/performance/health")
def health() -> dict[str, str]:
    return {"status": "ok", "service": "Performance Insight API"}


@router.get("/example")
@router.get("/api/performance/example", response_model=PerformanceAnalysisRequest)
def example() -> PerformanceAnalysisRequest:
    return analysis_service.create_demo_request()


@router.get("/api/performance/dashboard-data", response_model=PerformanceDashboardData)
def dashboard_data() -> PerformanceDashboardData:
    return _get_current_snapshot().dashboard


@router.get("/api/performance/current-report", response_model=PerformanceReportSnapshot)
def current_report() -> PerformanceReportSnapshot:
    return _get_current_snapshot()


@router.post("/api/performance/dashboard", response_model=PerformanceDashboardData)
def build_dashboard(request: PerformanceAnalysisRequest) -> PerformanceDashboardData:
    if not request.samples:
        raise HTTPException(status_code=400, detail="Samples cannot be empty.")

    snapshot = _create_snapshot(request.samples, "Carga manual", "API JSON")
    report_store.save(snapshot)
    return snapshot.dashboard


@router.post("/api/performance/jmeter/upload", response_model=PerformanceReportSnapshot)
async def upload_jmeter_results(file: UploadFile = File(...)) -> PerformanceReportSnapshot:
    if not file.filename:
        raise HTTPException(status_code=400, detail="Debes subir un archivo .jtl o .csv de JMeter.")

    file_bytes = await file.read()
    if not file_bytes:
        raise HTTPException(status_code=400, detail="Debes subir un archivo .jtl o .csv de JMeter.")

    try:
        samples = parser_service.parse_csv(file_bytes)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    if not samples:
        raise HTTPException(
            status_code=400,
            detail="No se pudieron extraer muestras validas del archivo de JMeter.",
        )

    snapshot = _create_snapshot(samples, file.filename, "JMeter CSV")
    report_store.save(snapshot)
    return snapshot


@router.post("/analyze")
@router.post("/api/performance/analyze", response_model=PerformanceAnalysisResult)
def analyze(request: PerformanceAnalysisRequest) -> PerformanceAnalysisResult:
    if not request.samples:
        raise HTTPException(status_code=400, detail="Samples cannot be empty.")

    return analysis_service.analyze(request.samples)


def _get_current_snapshot() -> PerformanceReportSnapshot:
    latest = report_store.get_latest()
    if latest is not None:
        return latest

    demo_request = analysis_service.create_demo_request()
    return _create_snapshot(demo_request.samples, "Datos simulados", "Demo")


def _create_snapshot(
    samples: list[PerformanceSample],
    source_name: str,
    source_type: str,
) -> PerformanceReportSnapshot:
    request = PerformanceAnalysisRequest(samples=samples)
    analysis = analysis_service.analyze(samples)
    dashboard = analysis_service.build_dashboard(samples, source_name, source_type)

    return PerformanceReportSnapshot(
        sourceType=source_type,
        sourceName=source_name,
        importedAtUtc=datetime.now(timezone.utc),
        request=request,
        analysis=analysis,
        dashboard=dashboard,
    )
