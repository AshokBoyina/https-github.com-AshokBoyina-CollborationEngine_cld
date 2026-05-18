namespace NICE.Platform.Collaboration.Infrastructure.Features.Collaborations.Commands.SupervisorJoin;

using MediatR;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.SupervisorJoin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Core.Exceptions;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public sealed class SupervisorJoinCommandHandler(
    CollaborationDbContext db,
    ILogger<SupervisorJoinCommandHandler> logger)
    : IRequestHandler<SupervisorJoinCommand, Unit>
{
    public async Task<Unit> Handle(
        SupervisorJoinCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "SupervisorJoin: collab={CollabId} supervisor={SupervisorId} silent={Silent}",
            request.CollaborationId, request.SupervisorId, request.IsSilent);

        var collab = await db.Collaborations.FindAsync(
            [request.CollaborationId], cancellationToken)
            ?? throw new CollaborationNotFoundException(request.CollaborationId);

        var existing = await db.Participants.FirstOrDefaultAsync(
            p => p.CollaborationId == request.CollaborationId
              && p.UserId          == request.SupervisorId
              && p.LeftAt          == null,
            cancellationToken);

        if (existing is null)
        {
            await db.Participants.AddAsync(new CollaborationParticipant
            {
                Id              = Guid.NewGuid(),
                CollaborationId = request.CollaborationId,
                UserId          = request.SupervisorId,
                UserType        = "Supervisor",
                JoinedAt        = DateTime.UtcNow,
                IsActiveAgent   = false
            }, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
