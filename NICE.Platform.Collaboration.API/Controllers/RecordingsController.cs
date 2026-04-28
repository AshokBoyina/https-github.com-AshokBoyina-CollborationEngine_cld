namespace NICE.Platform.Collaboration.API.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NICE.Platform.Collaboration.Contracts.Requests;

[ApiController]
[Route("api/recordings")]
public class RecordingsController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;
    
    [HttpPost("start")]
    public async Task<IActionResult> Start(
        [FromBody] StartRecordingRequest request, CancellationToken ct)
    {
        // TODO: dispatch StartRecordingCommand
        throw new NotImplementedException();
    }

    [HttpPost("{id:guid}/stop")]
    public async Task<IActionResult> Stop(Guid id, [FromQuery] string blobPath, CancellationToken ct)
    {
        // TODO: dispatch StopRecordingCommand
        throw new NotImplementedException();
    }

    [HttpGet("{collaborationId:guid}")]
    public async Task<IActionResult> GetByCollaboration(Guid collaborationId, CancellationToken ct)
    {
        // TODO: dispatch GetRecordingsByCollaborationQuery
        throw new NotImplementedException();
    }

    [HttpGet("{id:guid}/sas-url")]
    public async Task<IActionResult> GetSasUrl(Guid id, CancellationToken ct)
    {
        // TODO: dispatch GetRecordingSasUrlQuery
        throw new NotImplementedException();
    }
}
