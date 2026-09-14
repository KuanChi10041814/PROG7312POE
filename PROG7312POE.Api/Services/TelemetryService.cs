using Microsoft.EntityFrameworkCore;
using PROG7312POE.Api.Data;
using PROG7312POE.Core.DataStructures;
using PROG7312POE.Core.DTOs;
using PROG7312POE.Core.Models;

namespace PROG7312POE.Api.Services;

public class TelemetryService
{
    private readonly SmartXDbContext _dbContext;

    public TelemetryService(SmartXDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TelemetryResponse>> GetRecentAsync(int take = 50)
    {
        return await _dbContext.TelemetryRecords
            .Include(record => record.Sensor)
            .OrderByDescending(record => record.RecordedAtUtc)
            .Take(take)
            .Select(record => ToResponse(record))
            .ToListAsync();
    }

    public async Task<(TelemetryResponse? Telemetry, string? Error)> AddTelemetryAsync(TelemetryCreateRequest request)
    {
        var sensor = await _dbContext.Sensors.FindAsync(request.SensorId);
        if (sensor is null)
        {
            return (null, "Sensor was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.MetricName))
        {
            return (null, "Metric name is required.");
        }

        var recordedAtUtc = DateTime.UtcNow;
        var record = CreateRecord(request, recordedAtUtc);
        if (record is null)
        {
            return (null, "Value could not be parsed for the selected telemetry type.");
        }

        var batchBuffer = new TelemetryBatchBuffer(batchCount: 3, batchSize: 10);
        var stagedRecords = batchBuffer.Stage(record);
        _dbContext.TelemetryRecords.AddRange(stagedRecords);
        sensor.Status = record.IsAnomaly ? "Needs attention" : "Online";
        await _dbContext.SaveChangesAsync();

        record.Sensor = sensor;
        return (ToResponse(record), null);
    }

    public async Task<EngagementSummary> GetEngagementSummaryAsync()
    {
        var recentRecords = await _dbContext.TelemetryRecords
            .OrderByDescending(record => record.RecordedAtUtc)
            .Take(30)
            .ToListAsync();

        var offlineCount = await _dbContext.Sensors.CountAsync(sensor => sensor.Status != "Online");
        var anomalyCount = recentRecords.Count(record => record.IsAnomaly);

        var prompts = new List<string>();
        if (recentRecords.Any(record => record.Severity == "Critical"))
        {
            prompts.Add("Inspect the highlighted device first, then compare the last three telemetry packets for a spike.");
        }

        if (offlineCount > 0)
        {
            prompts.Add("Check power, Wi-Fi signal, and the deployment log for every sensor marked Needs attention.");
        }

        if (recentRecords.Any(record => record.MetricName.Contains("moisture", StringComparison.OrdinalIgnoreCase)))
        {
            prompts.Add("For moisture drops, verify pump state and inspect the most recent zone configuration upload.");
        }

        if (prompts.Count == 0)
        {
            prompts.Add("Telemetry is stable. Continue sampling mixed value types to prove the ingestion pipeline.");
        }

        return new EngagementSummary
        {
            TotalPackets = await _dbContext.TelemetryRecords.CountAsync(),
            AnomalyCount = anomalyCount,
            OfflineSensorCount = offlineCount,
            FocusMessage = anomalyCount > 0
                ? "Anomaly triage is active: focus on the highest-severity cards first."
                : "All recent packets are within the configured simulation thresholds.",
            TroubleshootingPrompts = prompts
        };
    }

    private static TelemetryRecord? CreateRecord(TelemetryCreateRequest request, DateTime recordedAtUtc)
    {
        return request.ValueType.Trim().ToLowerInvariant() switch
        {
            "int" or "integer" => int.TryParse(request.Value, out var intValue)
                ? FromPacket(new TelemetryPacket<int>(request.SensorId, request.MetricName.Trim(), intValue, recordedAtUtc))
                : null,
            "bool" or "boolean" => bool.TryParse(request.Value, out var boolValue)
                ? FromPacket(new TelemetryPacket<bool>(request.SensorId, request.MetricName.Trim(), boolValue, recordedAtUtc))
                : null,
            "double" or "float" or "decimal" => double.TryParse(request.Value, out var doubleValue)
                ? FromPacket(new TelemetryPacket<double>(request.SensorId, request.MetricName.Trim(), doubleValue, recordedAtUtc))
                : null,
            _ => FromPacket(new TelemetryPacket<string>(request.SensorId, request.MetricName.Trim(), request.Value.Trim(), recordedAtUtc))
        };
    }

    private static TelemetryRecord FromPacket<T>(TelemetryPacket<T> packet)
        where T : notnull
    {
        var (isAnomaly, severity, numericValue) = ScorePacket(packet);

        return new TelemetryRecord
        {
            SensorId = packet.SensorId,
            MetricName = packet.MetricName,
            ValueType = packet.ValueType,
            RawValue = packet.Value.ToString() ?? string.Empty,
            NumericValue = numericValue,
            IsAnomaly = isAnomaly,
            Severity = severity,
            RecordedAtUtc = packet.RecordedAtUtc
        };
    }

    private static (bool IsAnomaly, string Severity, double? NumericValue) ScorePacket<T>(TelemetryPacket<T> packet)
        where T : notnull
    {
        if (packet.Value is bool boolValue)
        {
            return (!boolValue, boolValue ? "Normal" : "Warning", null);
        }

        if (!double.TryParse(packet.Value.ToString(), out var numericValue))
        {
            return (false, "Normal", null);
        }

        var metric = packet.MetricName.ToLowerInvariant();
        var isCritical = metric.Contains("power") && numericValue > 1800
            || metric.Contains("temperature") && numericValue > 42
            || metric.Contains("moisture") && numericValue < 20;

        var isWarning = isCritical
            || numericValue < 0
            || numericValue > 1000 && !metric.Contains("power");

        return (isWarning, isCritical ? "Critical" : isWarning ? "Warning" : "Normal", numericValue);
    }

    private static TelemetryResponse ToResponse(TelemetryRecord record)
    {
        return new TelemetryResponse
        {
            Id = record.Id,
            SensorId = record.SensorId,
            SensorName = record.Sensor?.DeviceName ?? "Unknown sensor",
            MetricName = record.MetricName,
            ValueType = record.ValueType,
            RawValue = record.RawValue,
            NumericValue = record.NumericValue,
            IsAnomaly = record.IsAnomaly,
            Severity = record.Severity,
            RecordedAtUtc = record.RecordedAtUtc
        };
    }
}
