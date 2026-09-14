using Microsoft.EntityFrameworkCore;
using PROG7312POE.Api.Data;
using PROG7312POE.Core.DTOs;
using PROG7312POE.Core.Models;

namespace PROG7312POE.Api.Services;

public class AttachmentService
{
    private readonly SmartXDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public AttachmentService(SmartXDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    public async Task<List<AttachmentResponse>> GetForSensorAsync(int sensorId)
    {
        return await _dbContext.SensorAttachments
            .Where(attachment => attachment.SensorId == sensorId)
            .OrderByDescending(attachment => attachment.UploadedAtUtc)
            .Select(attachment => ToResponse(attachment))
            .ToListAsync();
    }

    public async Task<(AttachmentResponse? Attachment, string? Error)> SaveAsync(int sensorId, IFormFile file)
    {
        var sensorExists = await _dbContext.Sensors.AnyAsync(sensor => sensor.Id == sensorId);
        if (!sensorExists)
        {
            return (null, "Sensor was not found.");
        }

        if (file.Length == 0)
        {
            return (null, "The uploaded file is empty.");
        }

        var uploadRoot = Path.Combine(_environment.ContentRootPath, "Uploads", "SensorProfiles", sensorId.ToString());
        Directory.CreateDirectory(uploadRoot);

        var safeFileName = Path.GetFileName(file.FileName);
        var storedFileName = $"{Guid.NewGuid():N}_{safeFileName}";
        var storedPath = Path.Combine(uploadRoot, storedFileName);

        await using (var stream = File.Create(storedPath))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new SensorAttachment
        {
            SensorId = sensorId,
            FileName = safeFileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = file.Length,
            StoredPath = storedPath,
            UploadedAtUtc = DateTime.UtcNow
        };

        _dbContext.SensorAttachments.Add(attachment);
        await _dbContext.SaveChangesAsync();

        return (ToResponse(attachment), null);
    }

    private static AttachmentResponse ToResponse(SensorAttachment attachment)
    {
        return new AttachmentResponse
        {
            Id = attachment.Id,
            SensorId = attachment.SensorId,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            SizeBytes = attachment.SizeBytes,
            UploadedAtUtc = attachment.UploadedAtUtc
        };
    }
}
