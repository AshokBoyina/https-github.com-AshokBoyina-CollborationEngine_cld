namespace NICE.Platform.Collaboration.API.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NICE.Platform.Collaboration.Contracts.Requests;

[ApiController]
[Route("api/messages")]
public class MessagesController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;
    
    [HttpPost]
    public async Task<IActionResult> Send(
        [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        // TODO: dispatch SendMessageCommand
        throw new NotImplementedException();
    }

    [HttpGet("{collaborationId:guid}")]
    public async Task<IActionResult> GetByCollaboration(Guid collaborationId, CancellationToken ct)
    {
        // TODO: dispatch GetMessagesQuery
        throw new NotImplementedException();
    }
}
