namespace NICE.Platform.Collaboration.Application.Features.Recordings.Commands.StartRecording;
using MediatR;
public class StartRecordingCommandHandler : IRequestHandler<StartRecordingCommand, Guid>
{
    // TODO: inject ICollaborationRepository, ISignalRNotifier, etc. via constructor
    public Task<Guid> Handle(StartRecordingCommand request, CancellationToken cancellationToken)
    {
        // TODO: create CollaborationRecording entity, return recording id
        throw new NotImplementedException();
    }
}
