namespace NICE.Platform.Collaboration.Infrastructure.Features.Messages.Commands.SendMessage;

using MediatR;
using NICE.Platform.Collaboration.Application.Features.Messages.Commands.SendMessage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NICE.Platform.Collaboration.Core.Responses;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Core.Exceptions;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public sealed class SendMessageCommandHandler(
    CollaborationDbContext db,
    ILogger<SendMessageCommandHandler> logger)
    : IRequestHandler<SendMessageCommand, ChatMessageResponse>
{
    public async Task<ChatMessageResponse> Handle(
        SendMessageCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "SendMessage: collab={CollabId} sender={SenderId} type={Type}",
            request.CollaborationId, request.SenderId, request.MessageType);

        var collab = await db.Collaborations.FindAsync(
            [request.CollaborationId], cancellationToken)
            ?? throw new CollaborationNotFoundException(request.CollaborationId);

        // Look up sender's role from participant record (may be null for system messages)
        var senderType = await db.Participants
            .Where(p => p.CollaborationId == request.CollaborationId
                     && p.UserId          == request.SenderId
                     && p.LeftAt          == null)
            .Select(p => p.UserType)
            .FirstOrDefaultAsync(cancellationToken) ?? "Unknown";

        // Look up sender's display name from CollaborationUsers
        var senderUser = await db.Users
            .Where(u => u.Id == request.SenderId)
            .Select(u => new { u.FirstName, u.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        var senderName = senderUser is not null
            ? $"{senderUser.FirstName} {senderUser.LastName}".Trim()
            : "";

        var now = DateTime.UtcNow;
        var message = new CollaborationMessage
        {
            Id              = Guid.NewGuid(),
            CollaborationId = request.CollaborationId,
            SenderId        = request.SenderId,
            SenderType      = senderType,
            Body            = request.Content,
            MessageType     = request.MessageType,
            IsDeleted       = false,
            SentAt          = now
        };

        await db.Messages.AddAsync(message, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new ChatMessageResponse
        {
            Id              = message.Id,
            CollaborationId = request.CollaborationId,
            SenderId        = message.SenderId,
            SenderName      = senderName,
            SenderRole      = senderType,
            Content         = message.Body ?? string.Empty,
            SequenceNumber  = 0,
            ReplyToId       = request.ReplyToId,
            IsSystemNotice  = request.MessageType == "System",
            IsWhisper       = request.MessageType == "Whisper",
            SentAt          = message.SentAt
        };
    }
}
