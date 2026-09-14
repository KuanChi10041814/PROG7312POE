using Microsoft.AspNetCore.Mvc;
using PROG7312POE.Api.Services;
using PROG7312POE.Core.DTOs;

namespace PROG7312POE.Api.Controllers;

[ApiController]
[Route("api/sensors/{sensorId:int}/attachments")]
public class AttachmentsController : ControllerBase
{
    private readonly AttachmentService _attachmentService;

    public AttachmentsController(AttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AttachmentResponse>>> GetForSensor(int sensorId)
    {
        return await _attachmentService.GetForSensorAsync(sensorId);
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<AttachmentResponse>> Upload(int sensorId, IFormFile file)
    {
        var (attachment, error) = await _attachmentService.SaveAsync(sensorId, file);
        if (attachment is null)
        {
            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(GetForSensor), new { sensorId }, attachment);
    }
}
