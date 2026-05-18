namespace NICE.Platform.Collaboration.Infrastructure.Features.Users.Commands.SetAgentAvailability;

using MediatR;
using NICE.Platform.Collaboration.Application.Features.Users.Commands.SetAgentAvailability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public sealed class SetAgentAvailabilityCommandHandler(
    CollaborationDbContext db,
    ILogger<SetAgentAvailabilityCommandHandler> logger)
    : IRequestHandler<SetAgentAvailabilityCommand, Unit>
{
    public async Task<Unit> Handle(
        SetAgentAvailabilityCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "SetAgentAvailability: agent={AgentId} app={AppId} status={Status}",
            request.AgentId, request.ApplicationId, request.Status);

        // Update LastSeenAt and mark agent presence via CurrentSession
        var session = await db.CurrentSessions.FirstOrDefaultAsync(
            s => s.UserId == request.AgentId
              && s.ApplicationId == request.ApplicationId
              && s.UserType == "Agent",
            cancellationToken);

        if (session is not null)
        {
            session.LastSeenAt = DateTime.UtcNow;
            db.CurrentSessions.Update(session);
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            logger.LogWarning(
                "SetAgentAvailability: no active session for agent={AgentId} — ignoring.",
                request.AgentId);
        }

        return Unit.Value;
    }
}
