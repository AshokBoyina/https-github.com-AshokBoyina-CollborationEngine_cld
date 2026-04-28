namespace NICE.Platform.Collaboration.Application.Features.Recordings.Commands.StopRecording;
using MediatR;
public class StopRecordingCommandHandler : IRequestHandler<StopRecordingCommand, Unit>
{
    // TODO: inject ICollaborationRepository, ISignalRNotifier, etc. via constructor
    public Task<Unit> Handle(StopRecordingCommand request, CancellationToken cancellationToken)
    {
        // TODO: set EndedAt, store blob path, generate SAS and notify group
        throw new NotImplementedException();
    }
}
