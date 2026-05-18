namespace NICE.Platform.Collaboration.Infrastructure.Features.Collaborations.Commands.TransferCollaboration;

using MediatR;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.TransferCollaboration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Core.Exceptions;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public sealed class TransferCollaborationCommandHandler(
    CollaborationDbContext db,
    ILogger<TransferCollaborationCommandHandler> logger)
    : IRequestHandler<TransferCollaborationCommand, Unit>
{
    public async Task<Unit> Handle(
        TransferCollaborationCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "TransferCollaboration: collab={CollabId} from={From} to={To}",
            request.CollaborationId, request.FromAgentId, request.ToAgentId);

        var collab = await db.Collaborations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == request.CollaborationId, cancellationToken)
            ?? throw new CollaborationNotFoundException(request.CollaborationId);

        var now = DateTime.UtcNow;

        // Deactivate current agent participant
        var fromParticipant = collab.Participants.FirstOrDefault(
            p => p.UserId == request.FromAgentId && p.IsActiveAgent && p.LeftAt == null);
        if (fromParticipant is not null)
        {
            fromParticipant.IsActiveAgent = false;
            fromParticipant.LeftAt        = now;
            db.Participants.Update(fromParticipant);
        }

        // Record the transfer request
        await db.TransferRequests.AddAsync(new CollaborationTransferRequest
        {
            Id              = Guid.NewGuid(),
            CollaborationId = request.CollaborationId,
            FromUserId      = request.FromAgentId,
            ToUserId        = request.ToAgentId,
            TransferNote    = request.Reason,
            RequestedAt     = now,
            Status          = "Pending"
        }, cancellationToken);

        collab.Status = "Transferred";
        db.Collaborations.Update(collab);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
