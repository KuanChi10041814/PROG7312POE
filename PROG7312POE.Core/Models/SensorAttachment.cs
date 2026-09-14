namespace PROG7312POE.Core.Models;

public class SensorAttachment
{
    public int Id { get; set; }

    public int SensorId { get; set; }

    public Sensor? Sensor { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    public long SizeBytes { get; set; }

    public string StoredPath { get; set; } = string.Empty;

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
