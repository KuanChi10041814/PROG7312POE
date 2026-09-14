namespace PROG7312POE.Core.Models;

public class Sensor
{
    public int Id { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public string MacAddress { get; set; } = string.Empty;

    public string DeploymentLocation { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = "Online";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<TelemetryRecord> TelemetryRecords { get; set; } = new();

    public List<SensorAttachment> Attachments { get; set; } = new();
}
