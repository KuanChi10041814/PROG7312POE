using Microsoft.AspNetCore.Mvc;
using PROG7312POE.Api.Services;
using PROG7312POE.Core.DTOs;

namespace PROG7312POE.Api.Controllers;

[ApiController]
[Route("api/command-stream")]
public class CommandStreamController : ControllerBase
{
    private readonly CommandStreamService _commandStreamService;

    public CommandStreamController(CommandStreamService commandStreamService)
    {
        _commandStreamService = commandStreamService;
    }

    [HttpGet]
    public async Task<ActionResult<CommandStreamDashboard>> GetDashboard([FromQuery] string? searchTerm)
    {
        return await _commandStreamService.GetDashboardAsync(searchTerm);
    }

    [HttpPost("commands")]
    public async Task<ActionResult<CommandResponse>> IssueCommand(CommandCreateRequest request)
    {
        var (command, error) = await _commandStreamService.IssueCommandAsync(request);
        if (command is null)
        {
            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(GetDashboard), command);
    }

    [HttpPost("commands/undo")]
    public async Task<ActionResult<CommandResponse>> UndoLastCommand()
    {
        var (command, error) = await _commandStreamService.UndoLastCommandAsync();
        if (command is null)
        {
            return BadRequest(new { message = error });
        }

        return command;
    }
}
