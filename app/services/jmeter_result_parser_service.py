from __future__ import annotations

import csv
import io
from datetime import datetime, timezone

from app.models.performance_sample import PerformanceSample


class JMeterResultParserService:
    REQUIRED_COLUMNS = ("timestamp", "elapsed", "label", "success")

    def parse_csv(self, file_bytes: bytes) -> list[PerformanceSample]:
        if not file_bytes:
            return []

        text_stream = io.StringIO(file_bytes.decode("utf-8-sig", errors="replace"))
        reader = csv.reader(text_stream)

        try:
            raw_headers = next(reader)
        except StopIteration:
            return []

        headers = {
            self._normalize_header(header): index
            for index, header in enumerate(raw_headers)
            if self._normalize_header(header)
        }
        self._validate_headers(headers)

        samples: list[PerformanceSample] = []

        for row in reader:
            if not row:
                continue

            timestamp_text = self._get_field(row, headers, "timestamp")
            elapsed_text = self._get_field(row, headers, "elapsed")
            label = self._get_field(row, headers, "label")
            success_text = self._get_field(row, headers, "success")

            timestamp = self._parse_timestamp(timestamp_text)
            elapsed = self._parse_float(elapsed_text)
            success = self._parse_bool(success_text)

            if not label.strip() or timestamp is None or elapsed is None or success is None:
                continue

            samples.append(
                PerformanceSample(
                    endpoint=label.strip(),
                    timeMs=max(0, elapsed),
                    success=success,
                    timestamp=timestamp,
                    responseCode=self._get_field(row, headers, "responsecode"),
                    responseMessage=self._get_field(row, headers, "responsemessage"),
                    threadName=self._get_field(row, headers, "threadname"),
                )
            )

        return samples

    def _validate_headers(self, headers: dict[str, int]) -> None:
        missing = [column for column in self.REQUIRED_COLUMNS if column not in headers]
        if missing:
            raise ValueError(
                "El CSV de JMeter no contiene las columnas requeridas: "
                + ", ".join(missing)
                + "."
            )

    @staticmethod
    def _normalize_header(header: str) -> str:
        return header.strip().replace("_", "").replace(" ", "").lower()

    @staticmethod
    def _get_field(row: list[str], headers: dict[str, int], name: str) -> str:
        index = headers.get(name)
        if index is None or index >= len(row):
            return ""
        return row[index].strip()

    @staticmethod
    def _parse_float(value: str) -> float | None:
        try:
            return float(value)
        except (TypeError, ValueError):
            return None

    @staticmethod
    def _parse_bool(value: str) -> bool | None:
        normalized = value.strip().lower()
        if normalized == "true":
            return True
        if normalized == "false":
            return False
        return None

    @staticmethod
    def _parse_timestamp(value: str) -> datetime | None:
        raw = value.strip()
        if not raw:
            return None

        if raw.isdigit():
            try:
                return datetime.fromtimestamp(int(raw) / 1000, tz=timezone.utc)
            except (OverflowError, ValueError):
                return None

        iso_value = raw.replace("Z", "+00:00")
        try:
            parsed = datetime.fromisoformat(iso_value)
        except ValueError:
            return None

        if parsed.tzinfo is None:
            return parsed.replace(tzinfo=timezone.utc)

        return parsed.astimezone(timezone.utc)
