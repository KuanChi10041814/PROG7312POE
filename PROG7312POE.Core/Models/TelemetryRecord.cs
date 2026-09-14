namespace PROG7312POE.Core.Models;

public class TelemetryRecord
{
    public int Id { get; set; }

    public int SensorId { get; set; }

    public Sensor? Sensor { get; set; }

    public string MetricName { get; set; } = string.Empty;

    public string ValueType { get; set; } = string.Empty;

    public string RawValue { get; set; } = string.Empty;

    public double? NumericValue { get; set; }

    public bool IsAnomaly { get; set; }

    public string Severity { get; set; } = "Normal";

    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
