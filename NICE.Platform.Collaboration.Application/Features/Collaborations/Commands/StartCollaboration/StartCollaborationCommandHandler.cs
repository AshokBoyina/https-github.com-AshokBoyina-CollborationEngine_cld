namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.StartCollaboration;
using MediatR;
public class StartCollaborationCommandHandler : IRequestHandler<StartCollaborationCommand, NICE.Platform.Collaboration.Contracts.Responses.CollaborationResponse>
{
    // TODO: inject ICollaborationRepository, ISignalRNotifier, etc. via constructor
    public Task<NICE.Platform.Collaboration.Contracts.Responses.CollaborationResponse> Handle(StartCollaborationCommand request, CancellationToken cancellationToken)
    {
        // TODO: validate agent capacity, create Collaboration entity, create blob folder, notify agent via SignalR
        throw new NotImplementedException();
    }
}
