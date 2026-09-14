using Microsoft.EntityFrameworkCore;
using PROG7312POE.Api.Data;
using PROG7312POE.Core.DTOs;
using PROG7312POE.Core.Models;

namespace PROG7312POE.Api.Services;

public class SensorService
{
    private readonly SmartXDbContext _dbContext;

    public SensorService(SmartXDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<SensorResponse>> GetSensorsAsync()
    {
        return await _dbContext.Sensors
            .Include(sensor => sensor.Attachments)
            .OrderBy(sensor => sensor.DeviceName)
            .Select(sensor => ToResponse(sensor))
            .ToListAsync();
    }

    public async Task<SensorResponse?> GetSensorAsync(int id)
    {
        var sensor = await _dbContext.Sensors
            .Include(item => item.Attachments)
            .FirstOrDefaultAsync(item => item.Id == id);

        return sensor is null ? null : ToResponse(sensor);
    }

    public async Task<(SensorResponse? Sensor, string? Error)> RegisterSensorAsync(SensorCreateRequest request)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return (null, validationError);
        }

        var duplicateExists = await _dbContext.Sensors
            .AnyAsync(sensor => sensor.MacAddress == request.MacAddress.Trim());
        if (duplicateExists)
        {
            return (null, "A sensor with this MAC address is already registered.");
        }

        var deploymentTree = BuildDeploymentTree(request.DeploymentLocation);
        if (!DeploymentNode.ValidateRecursively(deploymentTree))
        {
            return (null, "Deployment path is incomplete. Use a path such as Facility A > Zone 1 > Sub-Zone B.");
        }

        var sensor = new Sensor
        {
            DeviceName = request.DeviceName.Trim(),
            MacAddress = request.MacAddress.Trim(),
            DeploymentLocation = request.DeploymentLocation.Trim(),
            Category = request.Category.Trim(),
            Status = "Online",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Sensors.Add(sensor);
        await _dbContext.SaveChangesAsync();

        return (ToResponse(sensor), null);
    }

    private static string? Validate(SensorCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceName))
        {
            return "Device name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.MacAddress) || request.MacAddress.Trim().Length < 8)
        {
            return "A valid MAC address or unique identifier is required.";
        }

        if (string.IsNullOrWhiteSpace(request.DeploymentLocation))
        {
            return "Deployment location is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return "Sensor category is required.";
        }

        return null;
    }

    private static DeploymentNode BuildDeploymentTree(string deploymentPath)
    {
        var parts = deploymentPath
            .Split('>', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        DeploymentNode? root = null;
        DeploymentNode? current = null;

        foreach (var part in parts)
        {
            var node = new DeploymentNode { Name = part, NodeType = "Deployment" };
            if (root is null)
            {
                root = node;
            }
            else
            {
                current!.Children.Add(node);
            }

            current = node;
        }

        return root ?? new DeploymentNode { Name = string.Empty, IsSafelyConfigured = false };
    }

    private static SensorResponse ToResponse(Sensor sensor)
    {
        return new SensorResponse
        {
            Id = sensor.Id,
            DeviceName = sensor.DeviceName,
            MacAddress = sensor.MacAddress,
            DeploymentLocation = sensor.DeploymentLocation,
            Category = sensor.Category,
            Status = sensor.Status,
            CreatedAtUtc = sensor.CreatedAtUtc,
            AttachmentCount = sensor.Attachments.Count
        };
    }
}
