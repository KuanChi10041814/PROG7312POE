namespace PROG7312POE.Core.Models;

public class CommandRecord
{
    public int Id { get; set; }

    public int SensorId { get; set; }

    public Sensor? Sensor { get; set; }

    public string CommandText { get; set; } = string.Empty;

    public string CommandType { get; set; } = "Manual Override";

    public bool IsUndo { get; set; }

    public bool IsUndone { get; set; }

    public string PreviousState { get; set; } = "Off";

    public string ResultingState { get; set; } = "Off";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
