namespace PROG7312POE.Core.Models;

public sealed class TelemetryPacket<T>
    where T : notnull
{
    public TelemetryPacket(int sensorId, string metricName, T value, DateTime recordedAtUtc)
    {
        SensorId = sensorId;
        MetricName = metricName;
        Value = value;
        RecordedAtUtc = recordedAtUtc;
    }

    public int SensorId { get; }

    public string MetricName { get; }

    public T Value { get; }

    public DateTime RecordedAtUtc { get; }

    public string ValueType => typeof(T).Name;
}
