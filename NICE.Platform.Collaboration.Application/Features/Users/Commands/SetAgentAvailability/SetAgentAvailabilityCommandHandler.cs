namespace NICE.Platform.Collaboration.Application.Features.Users.Commands.SetAgentAvailability;
using MediatR;
public class SetAgentAvailabilityCommandHandler : IRequestHandler<SetAgentAvailabilityCommand, Unit>
{
    // TODO: inject ICollaborationRepository, ISignalRNotifier, etc. via constructor
    public Task<Unit> Handle(SetAgentAvailabilityCommand request, CancellationToken cancellationToken)
    {
        // TODO: update AgentSession status in Redis and DB
        throw new NotImplementedException();
    }
}
