namespace NICE.Platform.Collaboration.Application.Features.Recordings.Commands.StartRecording;
using MediatR;
public record StartRecordingCommand(Guid CollaborationId, string Type) : IRequest<Guid>;
