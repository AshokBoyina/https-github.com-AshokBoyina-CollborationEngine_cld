namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.SupervisorJoin;
using MediatR;
public class SupervisorJoinCommandHandler : IRequestHandler<SupervisorJoinCommand, Unit>
{
    // TODO: inject ICollaborationRepository, ISignalRNotifier, etc. via constructor
    public Task<Unit> Handle(SupervisorJoinCommand request, CancellationToken cancellationToken)
    {
        // TODO: if not silent add to collab group and post system notice, if silent add to silent-monitor group only
        throw new NotImplementedException();
    }
}
