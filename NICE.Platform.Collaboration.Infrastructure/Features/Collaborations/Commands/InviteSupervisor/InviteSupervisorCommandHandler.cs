namespace NICE.Platform.Collaboration.Infrastructure.Features.Collaborations.Commands.InviteSupervisor;

using MediatR;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.InviteSupervisor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NICE.Platform.Collaboration.Core.Exceptions;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public sealed class InviteSupervisorCommandHandler(
    CollaborationDbContext db,
    ILogger<InviteSupervisorCommandHandler> logger)
    : IRequestHandler<InviteSupervisorCommand, Unit>
{
    public async Task<Unit> Handle(
        InviteSupervisorCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "InviteSupervisor: collab={CollabId} by={AgentId} supervisor={SupervisorId}",
            request.CollaborationId, request.InvitedByAgentId, request.SupervisorId);

        var collab = await db.Collaborations.FindAsync(
            [request.CollaborationId], cancellationToken)
            ?? throw new CollaborationNotFoundException(request.CollaborationId);

        // Verify supervisor is a member of the application
        var supervisorIsMember = await db.ApplicationUsers.AnyAsync(
            au => au.ApplicationId == collab.ApplicationId
               && au.UserId        == request.SupervisorId
               && au.IsActive,
            cancellationToken);

        if (!supervisorIsMember)
            logger.LogWarning(
                "Supervisor {SupervisorId} is not a registered member of application {AppId}.",
                request.SupervisorId, collab.ApplicationId);

        // The actual hub notification is handled by CollaborationHub after this command returns.
        return Unit.Value;
    }
}
