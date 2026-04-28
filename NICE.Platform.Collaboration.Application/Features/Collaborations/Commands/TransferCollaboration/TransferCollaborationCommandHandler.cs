namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.TransferCollaboration;
using MediatR;
public class TransferCollaborationCommandHandler : IRequestHandler<TransferCollaborationCommand, Unit>
{
    // TODO: inject ICollaborationRepository, ISignalRNotifier, etc. via constructor
    public Task<Unit> Handle(TransferCollaborationCommand request, CancellationToken cancellationToken)
    {
        // TODO: validate both agents belong to same application, create TransferRequest, notify target agent
        throw new NotImplementedException();
    }
}
