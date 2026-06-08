from datetime import datetime, timezone

from pydantic import BaseModel, Field


class EndpointLatencyPoint(BaseModel):
    endpoint: str = ""
    average_ms: float = Field(default=0, alias="averageMs")

    model_config = {"populate_by_name": True}


class LatencyTimelinePoint(BaseModel):
    label: str = ""
    p50_ms: float = Field(default=0, alias="p50Ms")
    p95_ms: float = Field(default=0, alias="p95Ms")
    max_ms: float = Field(default=0, alias="maxMs")

    model_config = {"populate_by_name": True}


class EndpointHealthPoint(BaseModel):
    endpoint: str = ""
    samples: int = 0
    error_count: int = Field(default=0, alias="errorCount")
    average_ms: float = Field(default=0, alias="averageMs")
    p95_ms: float = Field(default=0, alias="p95Ms")
    max_ms: float = Field(default=0, alias="maxMs")
    error_rate: float = Field(default=0, alias="errorRate")
    primary_response_code: str = Field(default="", alias="primaryResponseCode")

    model_config = {"populate_by_name": True}


class PerformanceIssue(BaseModel):
    severity: str = "Info"
    title: str = ""
    detail: str = ""
    endpoint: str = ""


class PerformanceDashboardData(BaseModel):
    generated_at_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc),
        alias="generatedAtUtc",
    )
    source_type: str = Field(default="Demo", alias="sourceType")
    source_name: str = Field(default="Datos simulados", alias="sourceName")
    total_samples: int = Field(default=0, alias="totalSamples")
    distinct_endpoints: int = Field(default=0, alias="distinctEndpoints")
    error_count: int = Field(default=0, alias="errorCount")
    average_ms: float = Field(default=0, alias="averageMs")
    p95_ms: float = Field(default=0, alias="p95Ms")
    max_ms: float = Field(default=0, alias="maxMs")
    throughput_per_second: float = Field(default=0, alias="throughputPerSecond")
    error_rate: float = Field(default=0, alias="errorRate")
    slowest_endpoint: str = Field(default="", alias="slowestEndpoint")
    status: str = ""
    status_message: str = Field(default="", alias="statusMessage")
    recommendations: list[str] = Field(default_factory=list)
    issues: list[PerformanceIssue] = Field(default_factory=list)
    endpoint_averages: list[EndpointLatencyPoint] = Field(
        default_factory=list,
        alias="endpointAverages",
    )
    endpoint_health: list[EndpointHealthPoint] = Field(
        default_factory=list,
        alias="endpointHealth",
    )
    timeline: list[LatencyTimelinePoint] = Field(default_factory=list)

    model_config = {"populate_by_name": True}
