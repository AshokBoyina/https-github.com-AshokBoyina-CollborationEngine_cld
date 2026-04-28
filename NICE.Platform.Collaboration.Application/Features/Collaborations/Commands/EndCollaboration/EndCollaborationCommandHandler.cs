namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.EndCollaboration;
using MediatR;
public class EndCollaborationCommandHandler : IRequestHandler<EndCollaborationCommand, Unit>
{
    // TODO: inject ICollaborationRepository, ISignalRNotifier, etc. via constructor
    public Task<Unit> Handle(EndCollaborationCommand request, CancellationToken cancellationToken)
    {
        // TODO: set status Ended, export chat to blob, decrement agent active count, notify group
        throw new NotImplementedException();
    }
}
