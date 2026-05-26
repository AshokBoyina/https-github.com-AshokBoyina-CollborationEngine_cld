namespace NICE.Platform.Collaboration.API.Controllers;

using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NICE.Platform.Collaboration.Application.Features.Messages.Commands.SendMessage;
using NICE.Platform.Collaboration.Application.Features.Messages.Queries.GetMessages;
using NICE.Platform.Collaboration.Core.Requests;

[Authorize]
[ApiController]
[Route("api/v1/collaboration/messages")]
public class MessagesController(ISender sender) : ControllerBase
{
    private Guid CallerId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub"), out var id)
            ? id : Guid.Empty;

    /// <summary>Send a message to a collaboration (REST alternative to the hub method).</summary>
    [HttpPost]
    public async Task<IActionResult> Send(
        [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new SendMessageCommand(
                request.CollaborationId,
                request.SenderId == Guid.Empty ? CallerId : request.SenderId,
                request.Content,
                request.ReplyToId), ct);
        return Ok(result);
    }

    /// <summary>Get all messages in a collaboration (ordered by sent time).</summary>
    [HttpGet("{collaborationId:guid}")]
    public async Task<IActionResult> GetByCollaboration(Guid collaborationId, CancellationToken ct)
    {
        var result = await sender.Send(new GetMessagesQuery(collaborationId), ct);
        return Ok(result);
    }
}
