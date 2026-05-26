namespace NICE.Platform.Collaboration.Infrastructure.Features.Messages.Queries.GetMessages;

using MediatR;
using NICE.Platform.Collaboration.Application.Features.Messages.Queries.GetMessages;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Core.Responses;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public sealed class GetMessagesQueryHandler(CollaborationDbContext db)
    : IRequestHandler<GetMessagesQuery, IEnumerable<ChatMessageResponse>>
{
    public async Task<IEnumerable<ChatMessageResponse>> Handle(
        GetMessagesQuery request, CancellationToken cancellationToken)
    {
        // Load messages ordered by time
        var messages = await db.Messages
            .AsNoTracking()
            .Where(m => m.CollaborationId == request.CollaborationId && !m.IsDeleted)
            .OrderBy(m => m.SentAt)
            .ToListAsync(cancellationToken);

        if (!messages.Any())
            return [];

        // Fetch distinct sender names in one round-trip
        var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
        var senders = await db.Users
            .AsNoTracking()
            .Where(u => senderIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim(), cancellationToken);

        return messages.Select(m => new ChatMessageResponse
        {
            Id              = m.Id,
            CollaborationId = m.CollaborationId,
            SenderId        = m.SenderId,
            SenderName      = senders.TryGetValue(m.SenderId, out var name) ? name : "Unknown",
            SenderRole      = m.SenderType,
            Content         = m.Body ?? string.Empty,
            IsSystemNotice  = m.MessageType == "System",
            IsWhisper       = m.MessageType == "Whisper",
            SentAt          = m.SentAt
        });
    }
}
