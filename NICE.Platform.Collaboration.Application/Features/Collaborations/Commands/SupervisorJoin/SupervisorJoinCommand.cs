namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.SupervisorJoin;
using MediatR;
public record SupervisorJoinCommand(
    Guid CollaborationId,
    Guid SupervisorId,
    bool IsSilent = false)
    : IRequest<Unit>;
