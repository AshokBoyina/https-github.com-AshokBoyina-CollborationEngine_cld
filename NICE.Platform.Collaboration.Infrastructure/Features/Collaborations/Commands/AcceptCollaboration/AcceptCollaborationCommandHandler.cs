namespace NICE.Platform.Collaboration.Infrastructure.Features.Collaborations.Commands.AcceptCollaboration;

using MediatR;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.AcceptCollaboration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NICE.Platform.Collaboration.Contracts.Responses;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Core.Exceptions;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public sealed class AcceptCollaborationCommandHandler(
    CollaborationDbContext db,
    ILogger<AcceptCollaborationCommandHandler> logger)
    : IRequestHandler<AcceptCollaborationCommand, CollaborationResponse>
{
    public async Task<CollaborationResponse> Handle(
        AcceptCollaborationCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "AcceptCollaboration: collab={CollabId} agent={AgentId}",
            request.CollaborationId, request.AgentId);

        var collab = await db.Collaborations.FindAsync(
            [request.CollaborationId], cancellationToken)
            ?? throw new CollaborationNotFoundException(request.CollaborationId);

        if (collab.Status == "Active")
            throw new InvalidOperationException(
                $"Collaboration {request.CollaborationId} is already active.");

        var now     = DateTime.UtcNow;
        collab.Status = "Active";
        db.Collaborations.Update(collab);

        // Add agent as active participant
        var existing = await db.Participants.FirstOrDefaultAsync(
            p => p.CollaborationId == request.CollaborationId
              && p.UserId          == request.AgentId
              && p.LeftAt          == null,
            cancellationToken);

        if (existing is null)
        {
            await db.Participants.AddAsync(new CollaborationParticipant
            {
                Id              = Guid.NewGuid(),
                CollaborationId = request.CollaborationId,
                UserId          = request.AgentId,
                UserType        = "Agent",
                JoinedAt        = now,
                IsActiveAgent   = true
            }, cancellationToken);
        }
        else
        {
            existing.IsActiveAgent = true;
            db.Participants.Update(existing);
        }

        // Update current session so agent knows which collab they're handling
        var session = await db.CurrentSessions.FirstOrDefaultAsync(
            s => s.UserId == request.AgentId, cancellationToken);
        if (session is not null)
        {
            session.CurrentCollaborationId = request.CollaborationId;
            db.CurrentSessions.Update(session);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CollaborationResponse
        {
            Id        = collab.Id,
            Status    = collab.Status,
            Type      = collab.ChatMode,
            StartedAt = collab.CreatedAt,
            EndedAt   = collab.EndedAt
        };
    }
}
