using System.Globalization;
using Microsoft.VisualBasic.FileIO;
using PerformanceInsight.Api.Models;

namespace PerformanceInsight.Api.Services;

public class JMeterResultParserService
{
    private static readonly string[] RequiredColumns = ["timestamp", "elapsed", "label", "success"];

    public List<PerformanceSample> ParseCsv(Stream stream)
    {
        using var parser = new TextFieldParser(stream);
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = false;

        if (parser.EndOfData)
        {
            return [];
        }

        var headerFields = parser.ReadFields();
        if (headerFields == null || headerFields.Length == 0)
        {
            return [];
        }

        var headers = headerFields
            .Select((header, index) => new
            {
                Header = NormalizeHeader(header),
                Index = index
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Header))
            .ToDictionary(item => item.Header, item => item.Index, StringComparer.OrdinalIgnoreCase);

        ValidateHeaders(headers);

        var samples = new List<PerformanceSample>();

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields == null || fields.Length == 0)
            {
                continue;
            }

            var timestampText = GetField(fields, headers, "timestamp");
            var elapsedText = GetField(fields, headers, "elapsed");
            var label = GetField(fields, headers, "label");
            var successText = GetField(fields, headers, "success");

            if (string.IsNullOrWhiteSpace(label) ||
                !TryParseTimestamp(timestampText, out var timestamp) ||
                !double.TryParse(elapsedText, NumberStyles.Float, CultureInfo.InvariantCulture, out var elapsedMs) ||
                !bool.TryParse(successText, out var success))
            {
                continue;
            }

            samples.Add(new PerformanceSample
            {
                Endpoint = label.Trim(),
                TimeMs = Math.Max(0, elapsedMs),
                Success = success,
                Timestamp = timestamp,
                ResponseCode = GetField(fields, headers, "responsecode"),
                ResponseMessage = GetField(fields, headers, "responsemessage"),
                ThreadName = GetField(fields, headers, "threadname")
            });
        }

        return samples;
    }

    private static void ValidateHeaders(IReadOnlyDictionary<string, int> headers)
    {
        var missing = RequiredColumns
            .Where(column => !headers.ContainsKey(column))
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"El CSV de JMeter no contiene las columnas requeridas: {string.Join(", ", missing)}.");
        }
    }

    private static string NormalizeHeader(string header)
    {
        return header
            .Trim()
            .Replace("_", "", StringComparison.Ordinal)
            .Replace(" ", "", StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private static string GetField(IReadOnlyList<string> fields, IReadOnlyDictionary<string, int> headers, string header)
    {
        return headers.TryGetValue(header, out var index) && index < fields.Count
            ? fields[index].Trim()
            : "";
    }

    private static bool TryParseTimestamp(string value, out DateTimeOffset timestamp)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixMilliseconds))
        {
            timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
            return true;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out timestamp))
        {
            return true;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
        {
            timestamp = new DateTimeOffset(dateTime.ToUniversalTime());
            return true;
        }

        timestamp = default;
        return false;
    }
}
