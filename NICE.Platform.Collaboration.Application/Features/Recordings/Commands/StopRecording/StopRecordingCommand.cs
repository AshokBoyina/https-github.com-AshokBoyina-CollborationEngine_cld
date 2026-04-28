namespace NICE.Platform.Collaboration.Application.Features.Recordings.Commands.StopRecording;
using MediatR;
public record StopRecordingCommand(Guid RecordingId, string BlobPath) : IRequest<Unit>;
