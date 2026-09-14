namespace PROG7312POE.Core.DTOs;

public sealed class AttachmentResponse
{
    public int Id { get; set; }

    public int SensorId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime UploadedAtUtc { get; set; }
}
