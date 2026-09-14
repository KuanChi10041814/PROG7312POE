using Microsoft.AspNetCore.Mvc;
using PROG7312POE.Api.Services;
using PROG7312POE.Core.DTOs;

namespace PROG7312POE.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly SensorService _sensorService;

    public SensorsController(SensorService sensorService)
    {
        _sensorService = sensorService;
    }

    [HttpGet]
    public async Task<ActionResult<List<SensorResponse>>> GetSensors()
    {
        return await _sensorService.GetSensorsAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SensorResponse>> GetSensor(int id)
    {
        var sensor = await _sensorService.GetSensorAsync(id);
        return sensor is null ? NotFound() : sensor;
    }

    [HttpPost]
    public async Task<ActionResult<SensorResponse>> RegisterSensor(SensorCreateRequest request)
    {
        var (sensor, error) = await _sensorService.RegisterSensorAsync(request);
        if (sensor is null)
        {
            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(GetSensor), new { id = sensor.Id }, sensor);
    }
}
