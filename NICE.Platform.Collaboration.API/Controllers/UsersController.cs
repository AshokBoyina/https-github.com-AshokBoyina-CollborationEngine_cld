namespace NICE.Platform.Collaboration.API.Controllers;

using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NICE.Platform.Collaboration.Application.Features.Users.Commands.OnboardUser;
using NICE.Platform.Collaboration.Application.Features.Users.Commands.SetAgentAvailability;
using NICE.Platform.Collaboration.Application.Features.Users.Queries.GetAvailableAgents;
using NICE.Platform.Collaboration.Application.Features.Users.Queries.GetAvailableSupervisors;
using NICE.Platform.Collaboration.Contracts.Requests;

[Authorize]
[ApiController]
[Route("api/v1/collaboration/users")]
public class UsersController(ISender sender) : ControllerBase
{
    private Guid CallerId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub"), out var id)
            ? id : Guid.Empty;

    /// <summary>
    /// Onboard (or re-onboard) a user into a specific application.
    /// Creates the user record if it doesn't exist; updates it if it does.
    /// </summary>
    [HttpPost("onboard")]
    public async Task<IActionResult> Onboard(
        [FromBody] OnboardUserRequest request,
        [FromQuery] Guid applicationId,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new OnboardUserCommand(
                request.ExternalId,
                request.Name,
                request.Email,
                applicationId,
                request.Role), ct);
        return Ok(result);
    }

    /// <summary>Get agents currently online and not in an active collaboration.</summary>
    [HttpGet("{applicationId:guid}/agents/available")]
    public async Task<IActionResult> GetAvailableAgents(Guid applicationId, CancellationToken ct)
    {
        var result = await sender.Send(new GetAvailableAgentsQuery(applicationId), ct);
        return Ok(result);
    }

    /// <summary>Get supervisors currently online for an application.</summary>
    [HttpGet("{applicationId:guid}/supervisors/available")]
    public async Task<IActionResult> GetAvailableSupervisors(Guid applicationId, CancellationToken ct)
    {
        var result = await sender.Send(new GetAvailableSupervisorsQuery(applicationId), ct);
        return Ok(result);
    }

    /// <summary>Agent sets their own availability status (Available | Busy | Away).</summary>
    [HttpPut("agents/availability")]
    public async Task<IActionResult> SetAvailability(
        [FromQuery] Guid applicationId,
        [FromQuery] string status,
        CancellationToken ct)
    {
        await sender.Send(
            new SetAgentAvailabilityCommand(CallerId, applicationId, status), ct);
        return NoContent();
    }
}
