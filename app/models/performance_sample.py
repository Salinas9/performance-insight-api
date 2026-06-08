from datetime import datetime, timezone

from pydantic import BaseModel, Field


class PerformanceSample(BaseModel):
    endpoint: str = ""
    time_ms: float = Field(default=0, alias="timeMs")
    success: bool = True
    timestamp: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    response_code: str = Field(default="", alias="responseCode")
    response_message: str = Field(default="", alias="responseMessage")
    thread_name: str = Field(default="", alias="threadName")

    model_config = {
        "populate_by_name": True,
        "json_schema_extra": {
            "example": {
                "endpoint": "/api/users",
                "timeMs": 245.0,
                "success": True,
                "timestamp": "2026-06-08T18:00:00Z",
                "responseCode": "200",
                "responseMessage": "OK",
                "threadName": "thread-1",
            }
        },
    }
