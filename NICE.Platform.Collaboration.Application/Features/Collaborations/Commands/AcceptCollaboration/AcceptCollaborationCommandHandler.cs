namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.AcceptCollaboration;
using MediatR;
public class AcceptCollaborationCommandHandler : IRequestHandler<AcceptCollaborationCommand, Unit>
{
    // TODO: inject ICollaborationRepository, ISignalRNotifier, etc. via constructor
    public Task<Unit> Handle(AcceptCollaborationCommand request, CancellationToken cancellationToken)
    {
        // TODO: set status Active, notify user group
        throw new NotImplementedException();
    }
}
