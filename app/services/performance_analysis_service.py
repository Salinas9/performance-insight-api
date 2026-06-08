from __future__ import annotations

import math
import random
from collections import Counter, defaultdict
from datetime import datetime, timedelta, timezone

from app.models.analysis_request import PerformanceAnalysisRequest
from app.models.analysis_result import PerformanceAnalysisResult
from app.models.dashboard_data import (
    EndpointHealthPoint,
    EndpointLatencyPoint,
    LatencyTimelinePoint,
    PerformanceDashboardData,
    PerformanceIssue,
)
from app.models.performance_sample import PerformanceSample


class PerformanceAnalysisService:
    def analyze(self, samples: list[PerformanceSample]) -> PerformanceAnalysisResult:
        times = self._get_ordered_times(samples)
        average = sum(times) / len(times)
        minimum = times[0]
        maximum = times[-1]
        p95 = self._get_percentile(times, 0.95)
        error_rate = self._get_error_rate(samples)
        endpoint_health = self._get_endpoint_health(samples)
        slowest_endpoint = max(endpoint_health, key=lambda item: item.average_ms).endpoint
        status, recommendations, _ = self._build_status_summary(
            average,
            p95,
            error_rate,
            slowest_endpoint,
            endpoint_health,
        )

        return PerformanceAnalysisResult(
            totalSamples=len(samples),
            averageMs=round(average, 2),
            minMs=round(minimum, 2),
            maxMs=round(maximum, 2),
            p95Ms=round(p95, 2),
            errorRate=round(error_rate, 2),
            slowestEndpoint=slowest_endpoint,
            status=status,
            recommendations=recommendations,
        )

    def build_dashboard(
        self,
        samples: list[PerformanceSample],
        source_name: str = "Datos simulados",
        source_type: str = "Demo",
    ) -> PerformanceDashboardData:
        times = self._get_ordered_times(samples)
        average = sum(times) / len(times)
        p95 = self._get_percentile(times, 0.95)
        maximum = times[-1]
        error_rate = self._get_error_rate(samples)
        endpoint_averages = self._get_endpoint_averages(samples)
        endpoint_health = self._get_endpoint_health(samples)
        slowest_endpoint = max(endpoint_health, key=lambda item: item.average_ms).endpoint
        throughput = self._get_throughput_per_second(samples)
        timeline = self._build_timeline(samples)
        status, recommendations, status_message = self._build_status_summary(
            average,
            p95,
            error_rate,
            slowest_endpoint,
            endpoint_health,
        )
        issues = self._build_issues(endpoint_health)

        generated_at = max(sample.timestamp for sample in samples).astimezone(timezone.utc)

        return PerformanceDashboardData(
            generatedAtUtc=generated_at,
            sourceType=source_type,
            sourceName=source_name,
            totalSamples=len(samples),
            distinctEndpoints=len({sample.endpoint.lower() for sample in samples}),
            errorCount=sum(1 for sample in samples if not sample.success),
            averageMs=round(average, 2),
            p95Ms=round(p95, 2),
            maxMs=round(maximum, 2),
            throughputPerSecond=round(throughput, 2),
            errorRate=round(error_rate, 2),
            slowestEndpoint=slowest_endpoint,
            status=status,
            statusMessage=status_message,
            recommendations=recommendations,
            issues=issues,
            endpointAverages=endpoint_averages,
            endpointHealth=endpoint_health,
            timeline=timeline,
        )

    def create_demo_request(self) -> PerformanceAnalysisRequest:
        generator = random.Random(42)
        start = datetime.now(timezone.utc) - timedelta(minutes=60)
        endpoints = [
            "/api/health",
            "/api/users",
            "/api/orders",
            "/api/network/analyze",
        ]

        samples: list[PerformanceSample] = []

        for minute in range(60):
            bucket_time = start + timedelta(minutes=minute)

            for index, endpoint in enumerate(endpoints):
                baseline = {
                    "/api/health": 120,
                    "/api/users": 210,
                    "/api/orders": 340,
                    "/api/network/analyze": 720,
                }.get(endpoint, 180)

                drift = 20 * math.sin(minute / 5.0 + index)
                jitter = generator.randint(-30, 44)
                spike = (
                    generator.randint(180, 419)
                    if endpoint == "/api/network/analyze" and minute % 11 == 0
                    else 0
                )
                time_ms = max(55, baseline + drift + jitter + spike)
                success = not (
                    endpoint == "/api/network/analyze"
                    and (minute % 14 == 0 or time_ms > 1000)
                )

                samples.append(
                    PerformanceSample(
                        endpoint=endpoint,
                        timeMs=round(time_ms, 2),
                        success=success,
                        timestamp=bucket_time + timedelta(seconds=index * 12),
                        responseCode="200" if success else "500",
                        responseMessage="OK" if success else "Server Error",
                        threadName=f"demo-thread-{index + 1}",
                    )
                )

        return PerformanceAnalysisRequest(samples=samples)

    @staticmethod
    def _get_ordered_times(samples: list[PerformanceSample]) -> list[float]:
        return sorted(sample.time_ms for sample in samples)

    @staticmethod
    def _get_percentile(ordered_values: list[float], percentile: float) -> float:
        index = math.ceil(len(ordered_values) * percentile) - 1
        index = max(0, min(index, len(ordered_values) - 1))
        return ordered_values[index]

    @staticmethod
    def _get_error_rate(samples: list[PerformanceSample]) -> float:
        return sum(1 for sample in samples if not sample.success) * 100 / len(samples)

    @staticmethod
    def _get_throughput_per_second(samples: list[PerformanceSample]) -> float:
        minimum = min(sample.timestamp for sample in samples)
        maximum = max(sample.timestamp for sample in samples)
        duration_seconds = max((maximum - minimum).total_seconds(), 1)
        return len(samples) / duration_seconds

    def _get_endpoint_averages(
        self,
        samples: list[PerformanceSample],
    ) -> list[EndpointLatencyPoint]:
        grouped: dict[str, list[PerformanceSample]] = defaultdict(list)
        for sample in samples:
            grouped[sample.endpoint].append(sample)

        points = [
            EndpointLatencyPoint(
                endpoint=endpoint,
                averageMs=round(
                    sum(item.time_ms for item in endpoint_samples) / len(endpoint_samples),
                    2,
                ),
            )
            for endpoint, endpoint_samples in grouped.items()
        ]

        return sorted(points, key=lambda point: point.average_ms)

    def _build_timeline(self, samples: list[PerformanceSample]) -> list[LatencyTimelinePoint]:
        ordered_samples = sorted(samples, key=lambda item: item.timestamp)
        minimum = ordered_samples[0].timestamp.astimezone(timezone.utc)
        maximum = ordered_samples[-1].timestamp.astimezone(timezone.utc)
        duration = maximum - minimum

        if duration.total_seconds() <= 120:
            bucket_seconds = 5
        elif duration.total_seconds() <= 600:
            bucket_seconds = 15
        elif duration.total_seconds() <= 3600:
            bucket_seconds = 60
        else:
            bucket_seconds = 300

        label_format = "%H:%M:%S" if bucket_seconds < 60 else "%H:%M"
        grouped: dict[datetime, list[PerformanceSample]] = defaultdict(list)

        for sample in ordered_samples:
            bucket = self._floor_timestamp(sample.timestamp.astimezone(timezone.utc), bucket_seconds)
            grouped[bucket].append(sample)

        timeline: list[LatencyTimelinePoint] = []
        for bucket, bucket_samples in sorted(grouped.items(), key=lambda item: item[0]):
            ordered_times = sorted(sample.time_ms for sample in bucket_samples)
            timeline.append(
                LatencyTimelinePoint(
                    label=bucket.strftime(label_format),
                    p50Ms=round(self._get_percentile(ordered_times, 0.50), 2),
                    p95Ms=round(self._get_percentile(ordered_times, 0.95), 2),
                    maxMs=round(ordered_times[-1], 2),
                )
            )

        return timeline

    def _get_endpoint_health(
        self,
        samples: list[PerformanceSample],
    ) -> list[EndpointHealthPoint]:
        grouped: dict[str, list[PerformanceSample]] = defaultdict(list)
        for sample in samples:
            grouped[sample.endpoint].append(sample)

        results: list[EndpointHealthPoint] = []

        for endpoint, endpoint_samples in grouped.items():
            ordered_times = sorted(sample.time_ms for sample in endpoint_samples)
            error_count = sum(1 for sample in endpoint_samples if not sample.success)
            primary_code = self._get_primary_response_code(endpoint_samples)

            results.append(
                EndpointHealthPoint(
                    endpoint=endpoint,
                    samples=len(endpoint_samples),
                    errorCount=error_count,
                    averageMs=round(sum(ordered_times) / len(ordered_times), 2),
                    p95Ms=round(self._get_percentile(ordered_times, 0.95), 2),
                    maxMs=round(ordered_times[-1], 2),
                    errorRate=round(error_count * 100 / len(endpoint_samples), 2),
                    primaryResponseCode=primary_code,
                )
            )

        return sorted(
            results,
            key=lambda item: (-item.error_rate, -item.p95_ms, -item.average_ms),
        )

    @staticmethod
    def _get_primary_response_code(samples: list[PerformanceSample]) -> str:
        codes = [sample.response_code for sample in samples if sample.response_code.strip()]
        if not codes:
            return ""

        counts = Counter(codes)
        return sorted(counts.items(), key=lambda item: (-item[1], item[0]))[0][0]

    def _build_issues(
        self,
        endpoint_health: list[EndpointHealthPoint],
    ) -> list[PerformanceIssue]:
        issues: list[PerformanceIssue] = []

        for endpoint in endpoint_health:
            if endpoint.error_rate >= 1:
                issues.append(
                    PerformanceIssue(
                        severity="Critical" if endpoint.error_rate >= 10 else "Warning",
                        title=f"Errores en {endpoint.endpoint}",
                        detail=(
                            f"{endpoint.error_count} fallos de {endpoint.samples} muestras "
                            f"({endpoint.error_rate:.2f}%). Codigo dominante: "
                            f"{endpoint.primary_response_code}."
                        ),
                        endpoint=endpoint.endpoint,
                    )
                )

            if endpoint.p95_ms >= 1000:
                issues.append(
                    PerformanceIssue(
                        severity="Critical" if endpoint.p95_ms >= 2000 else "Warning",
                        title=f"Latencia alta en {endpoint.endpoint}",
                        detail=(
                            f"P95 de {round(endpoint.p95_ms)} ms, media de "
                            f"{round(endpoint.average_ms)} ms y maximo de "
                            f"{round(endpoint.max_ms)} ms."
                        ),
                        endpoint=endpoint.endpoint,
                    )
                )

        if not issues:
            issues.append(
                PerformanceIssue(
                    severity="OK",
                    title="Sin incidencias destacadas",
                    detail="La ultima carga no presenta errores ni picos graves de latencia.",
                )
            )

        return sorted(
            issues,
            key=lambda issue: (-self._severity_rank(issue.severity), issue.title.lower()),
        )[:5]

    def _build_status_summary(
        self,
        average: float,
        p95: float,
        error_rate: float,
        slowest_endpoint: str,
        endpoint_health: list[EndpointHealthPoint],
    ) -> tuple[str, list[str], str]:
        recommendations: list[str] = []
        status = "OK"
        status_message = (
            "El rendimiento general es estable y no presenta degradaciones relevantes."
        )

        failing = next(
            (
                endpoint
                for endpoint in sorted(
                    endpoint_health,
                    key=lambda item: (-item.error_rate, -item.error_count),
                )
                if endpoint.error_count > 0
            ),
            None,
        )

        if p95 > 1000 or error_rate > 10:
            status = "Critical"
            status_message = (
                "El sistema presenta incidencias severas y requiere atencion inmediata."
            )
            recommendations.extend(
                [
                    f"Revisar la latencia extrema del endpoint {slowest_endpoint}.",
                    "Analizar posibles errores de servidor o cuellos de botella.",
                    "Investigar picos de saturacion en el percentil 95.",
                ]
            )
        elif average > 500 or error_rate > 2:
            status = "Warning"
            status_message = (
                "El rendimiento del sistema presenta degradaciones moderadas que requieren atencion."
            )
            recommendations.extend(
                [
                    f"Optimizar el endpoint mas lento: {slowest_endpoint}.",
                    "Reducir picos del percentil 95.",
                ]
            )
        else:
            recommendations.extend(
                [
                    "El rendimiento general es estable.",
                    "Mantener la vigilancia de endpoints criticos.",
                    "Seguir agregando muestras historicas.",
                ]
            )

        if failing is not None:
            recommendations.append(
                f"Investigar los fallos en {failing.endpoint} ({failing.error_rate:.2f}% de error)."
            )

        return status, recommendations, status_message

    @staticmethod
    def _floor_timestamp(value: datetime, bucket_seconds: int) -> datetime:
        timestamp = int(value.timestamp())
        floored = timestamp - (timestamp % bucket_seconds)
        return datetime.fromtimestamp(floored, tz=timezone.utc)

    @staticmethod
    def _severity_rank(severity: str) -> int:
        return {"Critical": 3, "Warning": 2, "OK": 1}.get(severity, 0)


analysis_service = PerformanceAnalysisService()
