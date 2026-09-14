namespace PROG7312POE.Core.DTOs;

public sealed class TelemetryCreateRequest
{
    public int SensorId { get; set; }

    public string MetricName { get; set; } = string.Empty;

    public string ValueType { get; set; } = "double";

    public string Value { get; set; } = string.Empty;
}

public sealed class TelemetryResponse
{
    public int Id { get; set; }

    public int SensorId { get; set; }

    public string SensorName { get; set; } = string.Empty;

    public string MetricName { get; set; } = string.Empty;

    public string ValueType { get; set; } = string.Empty;

    public string RawValue { get; set; } = string.Empty;

    public double? NumericValue { get; set; }

    public bool IsAnomaly { get; set; }

    public string Severity { get; set; } = "Normal";

    public DateTime RecordedAtUtc { get; set; }
}

public sealed class EngagementSummary
{
    public int TotalPackets { get; set; }

    public int AnomalyCount { get; set; }

    public int OfflineSensorCount { get; set; }

    public string FocusMessage { get; set; } = string.Empty;

    public List<string> TroubleshootingPrompts { get; set; } = new();
}
