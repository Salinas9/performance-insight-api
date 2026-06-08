from __future__ import annotations

from threading import Lock

from app.models.report_snapshot import PerformanceReportSnapshot


class PerformanceReportStore:
    def __init__(self) -> None:
        self._sync = Lock()
        self._latest_snapshot: PerformanceReportSnapshot | None = None

    def get_latest(self) -> PerformanceReportSnapshot | None:
        with self._sync:
            return self._latest_snapshot

    def save(self, snapshot: PerformanceReportSnapshot) -> None:
        with self._sync:
            self._latest_snapshot = snapshot


report_store = PerformanceReportStore()
