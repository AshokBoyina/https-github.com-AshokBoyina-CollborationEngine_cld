namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.InviteSupervisor;
using MediatR;
public record InviteSupervisorCommand(
    Guid  CollaborationId,
    Guid  InvitedByAgentId,
    Guid  SupervisorId)
    : IRequest<Unit>;
