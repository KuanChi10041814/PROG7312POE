namespace PROG7312POE.Core.DTOs;

public sealed class SensorCreateRequest
{
    public string DeviceName { get; set; } = string.Empty;

    public string MacAddress { get; set; } = string.Empty;

    public string DeploymentLocation { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
}

public sealed class SensorResponse
{
    public int Id { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public string MacAddress { get; set; } = string.Empty;

    public string DeploymentLocation { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public int AttachmentCount { get; set; }
}
