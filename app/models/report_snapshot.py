from datetime import datetime, timezone

from pydantic import BaseModel, Field

from app.models.analysis_request import PerformanceAnalysisRequest
from app.models.analysis_result import PerformanceAnalysisResult
from app.models.dashboard_data import PerformanceDashboardData


class PerformanceReportSnapshot(BaseModel):
    source_type: str = Field(default="", alias="sourceType")
    source_name: str = Field(default="", alias="sourceName")
    imported_at_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc),
        alias="importedAtUtc",
    )
    request: PerformanceAnalysisRequest = Field(default_factory=PerformanceAnalysisRequest)
    analysis: PerformanceAnalysisResult = Field(default_factory=PerformanceAnalysisResult)
    dashboard: PerformanceDashboardData = Field(default_factory=PerformanceDashboardData)

    model_config = {"populate_by_name": True}
