from pydantic import BaseModel, Field


class PerformanceAnalysisResult(BaseModel):
    total_samples: int = Field(default=0, alias="totalSamples")
    average_ms: float = Field(default=0, alias="averageMs")
    min_ms: float = Field(default=0, alias="minMs")
    max_ms: float = Field(default=0, alias="maxMs")
    p95_ms: float = Field(default=0, alias="p95Ms")
    error_rate: float = Field(default=0, alias="errorRate")
    slowest_endpoint: str = Field(default="", alias="slowestEndpoint")
    status: str = ""
    recommendations: list[str] = Field(default_factory=list)

    model_config = {"populate_by_name": True}
