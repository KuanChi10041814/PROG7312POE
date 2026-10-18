namespace PROG7312POE.Core.DTOs;

public sealed class CommandCreateRequest
{
    public int SensorId { get; set; }

    public string CommandText { get; set; } = string.Empty;

    public string CommandType { get; set; } = "Manual Override";
}

public sealed class CommandResponse
{
    public int Id { get; set; }

    public int SensorId { get; set; }

    public string SensorName { get; set; } = string.Empty;

    public string CommandText { get; set; } = string.Empty;

    public string CommandType { get; set; } = string.Empty;

    public bool IsUndo { get; set; }

    public bool IsUndone { get; set; }

    public string ResultingState { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CommandStreamDashboard
{
    public int StandardQueueCount { get; set; }

    public int CriticalQueueCount { get; set; }

    public int CommandHistoryCount { get; set; }

    public string SearchTerm { get; set; } = string.Empty;

    public List<SensorResponse> Devices { get; set; } = new();

    public List<TelemetryResponse> StandardTelemetryQueue { get; set; } = new();

    public List<TelemetryResponse> CriticalAlerts { get; set; } = new();

    public List<CommandResponse> CommandHistory { get; set; } = new();

    public List<TimelineTelemetryBucket> Timeline { get; set; } = new();

    public List<string> ActiveErrorStates { get; set; } = new();

    public List<SuggestedAction> SuggestedActions { get; set; } = new();
}

public sealed class TimelineTelemetryBucket
{
    public DateTime TimestampUtc { get; set; }

    public List<TelemetryResponse> Packets { get; set; } = new();
}

public sealed class SuggestedAction
{
    public int SensorId { get; set; }

    public string SensorName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string CommandText { get; set; } = string.Empty;
}
