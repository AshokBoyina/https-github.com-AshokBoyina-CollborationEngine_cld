namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.InviteSupervisor;
using MediatR;
public class InviteSupervisorCommandHandler : IRequestHandler<InviteSupervisorCommand, Unit>
{
    // TODO: inject ICollaborationRepository, ISignalRNotifier, etc. via constructor
    public Task<Unit> Handle(InviteSupervisorCommand request, CancellationToken cancellationToken)
    {
        // TODO: validate supervisor belongs to same application, send SignalR notification to supervisor
        throw new NotImplementedException();
    }
}
