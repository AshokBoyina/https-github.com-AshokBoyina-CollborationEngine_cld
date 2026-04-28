namespace NICE.Platform.Collaboration.API.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NICE.Platform.Collaboration.Contracts.Requests;

[ApiController]
[Route("api/collaborations")]
public class CollaborationsController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;
    
    [HttpPost]
    public async Task<IActionResult> Start(
        [FromBody] StartCollaborationRequest request, CancellationToken ct)
    {
        // TODO: dispatch StartCollaborationCommand
        throw new NotImplementedException();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        // TODO: dispatch GetCollaborationByIdQuery
        throw new NotImplementedException();
    }

    [HttpGet("active/{applicationId:guid}")]
    public async Task<IActionResult> GetActive(Guid applicationId, CancellationToken ct)
    {
        // TODO: dispatch GetActiveCollaborationsQuery
        throw new NotImplementedException();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> End(Guid id, CancellationToken ct)
    {
        // TODO: dispatch EndCollaborationCommand
        throw new NotImplementedException();
    }

    [HttpPost("{id:guid}/transfer")]
    public async Task<IActionResult> Transfer(Guid id,
        [FromBody] TransferCollaborationRequest request, CancellationToken ct)
    {
        // TODO: dispatch TransferCollaborationCommand
        throw new NotImplementedException();
    }

    [HttpPost("{id:guid}/invite-supervisor")]
    public async Task<IActionResult> InviteSupervisor(Guid id,
        [FromQuery] Guid supervisorId, CancellationToken ct)
    {
        // TODO: dispatch InviteSupervisorCommand
        throw new NotImplementedException();
    }
}
