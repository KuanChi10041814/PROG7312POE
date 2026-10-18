using Microsoft.EntityFrameworkCore;
using PROG7312POE.Api.Data;
using PROG7312POE.Core.DTOs;
using PROG7312POE.Core.Models;

namespace PROG7312POE.Api.Services;

public class CommandStreamService
{
    private readonly SmartXDbContext _dbContext;

    public CommandStreamService(SmartXDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CommandStreamDashboard> GetDashboardAsync(string? searchTerm)
    {
        var sensors = await _dbContext.Sensors
            .AsNoTracking()
            .OrderBy(sensor => sensor.DeviceName)
            .ToListAsync();

        var recentTelemetry = await _dbContext.TelemetryRecords
            .AsNoTracking()
            .Include(record => record.Sensor)
            .OrderByDescending(record => record.RecordedAtUtc)
            .Take(80)
            .ToListAsync();

        var recentCommands = await _dbContext.CommandRecords
            .AsNoTracking()
            .Include(command => command.Sensor)
            .OrderByDescending(command => command.CreatedAtUtc)
            .Take(30)
            .ToListAsync();

        var devicesByMac = sensors.ToDictionary(sensor => sensor.MacAddress, StringComparer.OrdinalIgnoreCase);
        var devicesById = sensors.ToDictionary(sensor => sensor.Id);
        var standardQueue = new Queue<TelemetryRecord>();
        var criticalQueue = new PriorityQueue<TelemetryRecord, int>();
        var commandUndoStack = new Stack<CommandRecord>(recentCommands.Where(command => !command.IsUndo && !command.IsUndone).OrderBy(command => command.Id));
        var timeline = new SortedDictionary<DateTime, List<TelemetryRecord>>();
        var activeErrorStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var latestMetrics = recentTelemetry.GroupBy(record => (record.SensorId, record.MetricName.ToLowerInvariant()))
            .Select(group => group.OrderByDescending(record => record.Id).First()).ToList();
        foreach (var packet in recentTelemetry.OrderBy(record => record.RecordedAtUtc))
        {
            if (packet.Severity == "Critical" || packet.Severity == "Warning")
            {
                criticalQueue.Enqueue(packet, packet.Severity == "Critical" ? 0 : 1);
            }
            else
            {
                standardQueue.Enqueue(packet);
            }

            var bucketKey = new DateTime(
                packet.RecordedAtUtc.Year,
                packet.RecordedAtUtc.Month,
                packet.RecordedAtUtc.Day,
                packet.RecordedAtUtc.Hour,
                packet.RecordedAtUtc.Minute,
                0,
                DateTimeKind.Utc);
            if (!timeline.TryGetValue(bucketKey, out var bucket))
            {
                bucket = new List<TelemetryRecord>();
                timeline[bucketKey] = bucket;
            }

            bucket.Add(packet);
        }

        foreach (var packet in latestMetrics.Where(record => record.IsAnomaly))
            activeErrorStates.Add($"{packet.Sensor?.DeviceName ?? "Unknown"}: {packet.MetricName} {packet.Severity}");

        foreach (var sensor in sensors.Where(sensor => sensor.Status != "Online"))
        {
            activeErrorStates.Add($"{sensor.DeviceName}: {sensor.Status}");
        }

        var filteredDevices = FilterDevices(sensors, activeErrorStates, searchTerm);
        var criticalQueueCount = criticalQueue.Count;
        var criticalAlerts = DrainPriorityQueue(criticalQueue)
            .Where(record => MatchesSearch(record, searchTerm))
            .Take(10)
            .Select(ToTelemetryResponse)
            .ToList();
        var standardTelemetry = standardQueue
            .Where(record => MatchesSearch(record, searchTerm))
            .Take(10)
            .Select(ToTelemetryResponse)
            .ToList();

        return new CommandStreamDashboard
        {
            StandardQueueCount = standardQueue.Count,
            CriticalQueueCount = criticalQueueCount,
            CommandHistoryCount = commandUndoStack.Count,
            SearchTerm = searchTerm ?? string.Empty,
            Devices = filteredDevices.Select(ToSensorResponse).ToList(),
            StandardTelemetryQueue = standardTelemetry,
            CriticalAlerts = criticalAlerts,
            CommandHistory = recentCommands.Select(ToCommandResponse).ToList(),
            Timeline = timeline
                .Reverse()
                .Take(12)
                .Select(pair => new TimelineTelemetryBucket
                {
                    TimestampUtc = pair.Key,
                    Packets = pair.Value.Select(ToTelemetryResponse).ToList()
                })
                .ToList(),
            ActiveErrorStates = activeErrorStates.OrderBy(item => item).ToList(),
            SuggestedActions = BuildSuggestedActions(devicesByMac, devicesById, latestMetrics, recentCommands, searchTerm)
        };
    }

    public async Task<(CommandResponse? Command, string? Error)> IssueCommandAsync(CommandCreateRequest request)
    {
        var sensor = await _dbContext.Sensors.FindAsync(request.SensorId);
        if (sensor is null)
        {
            return (null, "Sensor was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.CommandText))
        {
            return (null, "Command text is required.");
        }

        var command = new CommandRecord
        {
            SensorId = request.SensorId,
            CommandText = request.CommandText.Trim(),
            CommandType = string.IsNullOrWhiteSpace(request.CommandType) ? "Manual Override" : request.CommandType.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            PreviousState = sensor.ControlState,
            ResultingState = ApplyCommand(sensor.ControlState, request.CommandText)
        };

        sensor.ControlState = command.ResultingState;

        _dbContext.CommandRecords.Add(command);
        await _dbContext.SaveChangesAsync();

        command.Sensor = sensor;
        return (ToCommandResponse(command), null);
    }

    public async Task<(CommandResponse? Command, string? Error)> UndoLastCommandAsync()
    {
        var commands = await _dbContext.CommandRecords
            .Include(command => command.Sensor)
            .Where(command => !command.IsUndo && !command.IsUndone)
            .OrderBy(command => command.Id)
            .ToListAsync();

        var undoStack = new Stack<CommandRecord>(commands);
        if (!undoStack.TryPop(out var lastCommand))
        {
            return (null, "No command is available to undo.");
        }

        var undoCommand = new CommandRecord
        {
            SensorId = lastCommand.SensorId,
            CommandText = $"UNDO: {lastCommand.CommandText}",
            CommandType = "Undo",
            IsUndo = true,
            CreatedAtUtc = DateTime.UtcNow,
            PreviousState = lastCommand.ResultingState,
            ResultingState = lastCommand.PreviousState
        };

        lastCommand.IsUndone = true;
        lastCommand.Sensor!.ControlState = lastCommand.PreviousState;
        _dbContext.CommandRecords.Add(undoCommand);
        await _dbContext.SaveChangesAsync();

        undoCommand.Sensor = lastCommand.Sensor;
        return (ToCommandResponse(undoCommand), null);
    }

    private static string ApplyCommand(string state, string text)
    {
        var command = text.Trim().ToLowerInvariant();
        if (command.Contains("toggle")) return state == "On" ? "Off" : "On";
        if (command.Contains("stop") || command.Contains("close") || command.Contains("shut")
            || command.Contains("disable") || command.Contains("turn off")) return "Off";
        if (command.Contains("start") || command.Contains("open") || command.Contains("enable")
            || command.Contains("turn on")) return "On";
        if (command.Contains("throttle")) return "Throttled";
        return state;
    }

    private static List<Sensor> FilterDevices(List<Sensor> sensors, HashSet<string> activeErrorStates, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return sensors;
        }

        var term = searchTerm.Trim();
        return sensors
            .Where(sensor =>
                sensor.DeviceName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || sensor.MacAddress.Contains(term, StringComparison.OrdinalIgnoreCase)
                || sensor.Category.Contains(term, StringComparison.OrdinalIgnoreCase)
                || sensor.Status.Contains(term, StringComparison.OrdinalIgnoreCase)
                || activeErrorStates.Any(error => error.Contains(sensor.DeviceName, StringComparison.OrdinalIgnoreCase)
                    && error.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static List<TelemetryRecord> DrainPriorityQueue(PriorityQueue<TelemetryRecord, int> queue)
    {
        var ordered = new List<TelemetryRecord>();
        while (queue.TryDequeue(out var item, out _))
        {
            ordered.Add(item);
        }

        return ordered;
    }

    private static bool MatchesSearch(TelemetryRecord record, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return true;
        }

        var term = searchTerm.Trim();
        return record.MetricName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || record.Severity.Contains(term, StringComparison.OrdinalIgnoreCase)
            || record.Sensor?.DeviceName.Contains(term, StringComparison.OrdinalIgnoreCase) == true
            || record.Sensor?.Category.Contains(term, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static List<SuggestedAction> BuildSuggestedActions(
        Dictionary<string, Sensor> devicesByMac,
        Dictionary<int, Sensor> devicesById,
        List<TelemetryRecord> telemetry,
        List<CommandRecord> commands,
        string? searchTerm)
    {
        var suggestions = new List<SuggestedAction>();
        var recentAnomalies = telemetry
            .Where(record => record.IsAnomaly)
            .OrderByDescending(record => record.RecordedAtUtc)
            .Take(8)
            .ToList();

        foreach (var anomaly in recentAnomalies)
        {
            if (!devicesById.TryGetValue(anomaly.SensorId, out var sensor))
            {
                continue;
            }

            var commandText = anomaly.MetricName.Contains("moisture", StringComparison.OrdinalIgnoreCase)
                ? "Start pump for 30 seconds"
                : anomaly.MetricName.Contains("power", StringComparison.OrdinalIgnoreCase)
                    ? "Throttle relay load"
                    : anomaly.MetricName.Contains("temperature", StringComparison.OrdinalIgnoreCase)
                        ? "Enable cooling cycle"
                        : "Run diagnostic check";

            suggestions.Add(new SuggestedAction
            {
                SensorId = sensor.Id,
                SensorName = sensor.DeviceName,
                Title = $"{anomaly.Severity} {anomaly.MetricName} response",
                Reason = $"{sensor.DeviceName} recently reported {anomaly.RawValue}.",
                CommandText = commandText
            });
        }

        var frequentCommand = commands
            .Where(command => !command.IsUndo)
            .GroupBy(command => command.CommandText)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();
        if (frequentCommand is not null && frequentCommand.Count() >= 2 && commands.FirstOrDefault(command => command.CommandText == frequentCommand.Key) is { } sample)
        {
            suggestions.Add(new SuggestedAction
            {
                SensorId = sample.SensorId,
                SensorName = sample.Sensor?.DeviceName ?? "Recent device",
                Title = "Repeat frequent override",
                Reason = $"Operators used this command {frequentCommand.Count()} times recently.",
                CommandText = frequentCommand.Key
            });
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchedDevice = devicesByMac.Values.FirstOrDefault(sensor =>
                sensor.DeviceName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                || sensor.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            if (searchedDevice is not null)
            {
                suggestions.Add(new SuggestedAction
                {
                    SensorId = searchedDevice.Id,
                    SensorName = searchedDevice.DeviceName,
                    Title = "Search-linked inspection",
                    Reason = $"Your filter matches {searchedDevice.Category}; inspect it before issuing an override.",
                    CommandText = "Run diagnostic check"
                });
            }
        }

        return suggestions
            .GroupBy(suggestion => $"{suggestion.SensorId}:{suggestion.CommandText}")
            .Select(group => group.First())
            .Take(6)
            .ToList();
    }

    private static SensorResponse ToSensorResponse(Sensor sensor)
    {
        return new SensorResponse
        {
            Id = sensor.Id,
            DeviceName = sensor.DeviceName,
            MacAddress = sensor.MacAddress,
            DeploymentLocation = sensor.DeploymentLocation,
            Category = sensor.Category,
            Status = sensor.Status,
            ControlState = sensor.ControlState,
            CreatedAtUtc = sensor.CreatedAtUtc,
            AttachmentCount = sensor.Attachments.Count
        };
    }

    private static TelemetryResponse ToTelemetryResponse(TelemetryRecord record)
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

    private static CommandResponse ToCommandResponse(CommandRecord command)
    {
        return new CommandResponse
        {
            Id = command.Id,
            SensorId = command.SensorId,
            SensorName = command.Sensor?.DeviceName ?? "Unknown sensor",
            CommandText = command.CommandText,
            CommandType = command.CommandType,
            IsUndo = command.IsUndo,
            IsUndone = command.IsUndone,
            ResultingState = command.ResultingState,
            CreatedAtUtc = command.CreatedAtUtc
        };
    }
}
