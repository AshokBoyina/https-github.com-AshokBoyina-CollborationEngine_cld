namespace NICE.Platform.Collaboration.API.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NICE.Platform.Collaboration.Contracts.Requests;

[ApiController]
[Route("api/users")]
public class UsersController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;
    
    [HttpPost("onboard")]
    public async Task<IActionResult> Onboard(
        [FromBody] OnboardUserRequest request, CancellationToken ct)
    {
        // TODO: map to OnboardUserCommand and dispatch
        throw new NotImplementedException();
    }

    [HttpGet("{applicationId:guid}/agents/available")]
    public async Task<IActionResult> GetAvailableAgents(Guid applicationId, CancellationToken ct)
    {
        // TODO: dispatch GetAvailableAgentsQuery
        throw new NotImplementedException();
    }

    [HttpGet("{applicationId:guid}/supervisors/available")]
    public async Task<IActionResult> GetAvailableSupervisors(Guid applicationId, CancellationToken ct)
    {
        // TODO: dispatch GetAvailableSupervisorsQuery
        throw new NotImplementedException();
    }
}
