from pydantic import BaseModel, Field

from app.models.performance_sample import PerformanceSample


class PerformanceAnalysisRequest(BaseModel):
    samples: list[PerformanceSample] = Field(default_factory=list)
