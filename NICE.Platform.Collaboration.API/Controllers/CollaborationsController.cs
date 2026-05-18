namespace NICE.Platform.Collaboration.API.Controllers;

using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.EndCollaboration;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.InviteSupervisor;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.StartCollaboration;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.SupervisorJoin;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.TransferCollaboration;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Queries.GetActiveCollaborations;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Queries.GetCollaborationById;
using NICE.Platform.Collaboration.Contracts.Requests;

[Authorize]
[ApiController]
[Route("api/v1/collaboration/collaborations")]
public class CollaborationsController(ISender sender) : ControllerBase
{
    private Guid CallerId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub"), out var id)
            ? id : Guid.Empty;

    /// <summary>Start a new collaboration (REST alternative to the SignalR hub method).</summary>
    [HttpPost]
    public async Task<IActionResult> Start(
        [FromBody] StartCollaborationRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new StartCollaborationCommand(request.UserId, request.PreferredAgentId, request.ApplicationId), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Get a specific collaboration by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCollaborationByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Get all active collaborations for an application.</summary>
    [HttpGet("active/{applicationId:guid}")]
    public async Task<IActionResult> GetActive(Guid applicationId, CancellationToken ct)
    {
        var result = await sender.Send(new GetActiveCollaborationsQuery(applicationId), ct);
        return Ok(result);
    }

    /// <summary>End a collaboration.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> End(
        Guid id, [FromQuery] string reason = "Completed", CancellationToken ct = default)
    {
        var result = await sender.Send(new EndCollaborationCommand(id, CallerId, reason), ct);
        return Ok(result);
    }

    /// <summary>Transfer collaboration to another agent.</summary>
    [HttpPost("{id:guid}/transfer")]
    public async Task<IActionResult> Transfer(
        Guid id, [FromBody] TransferCollaborationRequest request, CancellationToken ct)
    {
        await sender.Send(
            new TransferCollaborationCommand(id, CallerId, request.ToAgentId, request.Reason), ct);
        return Accepted();
    }

    /// <summary>Invite a supervisor to an active collaboration.</summary>
    [HttpPost("{id:guid}/invite-supervisor")]
    public async Task<IActionResult> InviteSupervisor(
        Guid id, [FromQuery] Guid supervisorId, CancellationToken ct)
    {
        await sender.Send(new InviteSupervisorCommand(id, CallerId, supervisorId), ct);
        return Accepted();
    }

    /// <summary>Supervisor explicitly joins a collaboration.</summary>
    [HttpPost("{id:guid}/supervisor-join")]
    public async Task<IActionResult> SupervisorJoin(
        Guid id, [FromQuery] bool silent = false, CancellationToken ct = default)
    {
        await sender.Send(new SupervisorJoinCommand(id, CallerId, silent), ct);
        return Accepted();
    }
}
