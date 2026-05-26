namespace NICE.Platform.Collaboration.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NICE.Platform.Collaboration.Application.Features.Applications.Commands.RegisterApplication;
using NICE.Platform.Collaboration.Core.Requests;

[Authorize]
[ApiController]
[Route("api/v1/collaboration/applications")]
public class ApplicationsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Register a new application in the collaboration engine.
    /// Returns the new application ID; store this to identify the tenant in all subsequent calls.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterApplicationRequest request, CancellationToken ct)
    {
        var id = await sender.Send(
            new RegisterApplicationCommand(
                request.Name,
                request.MaxAgentsOnline,
                request.MaxUsersOnline,
                request.MaxCollaborationsPerAgent,
                request.WebhookUrl), ct);

        return CreatedAtAction(nameof(Register), new { id }, new { Id = id });
    }
}
