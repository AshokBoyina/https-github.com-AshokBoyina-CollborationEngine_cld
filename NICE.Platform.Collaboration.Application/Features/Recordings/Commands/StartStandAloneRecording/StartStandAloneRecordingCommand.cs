namespace NICE.Platform.Collaboration.Application.Features.Recordings.Commands.StartStandAloneRecording;

using MediatR;
using NICE.Platform.Collaboration.Contracts.Responses;

/// <summary>
/// Creates a StandAlone Collaboration + a Recording row for an agent who
/// initiates a screen-capture session without an external customer present.
/// Issued by RecordingHub when the agent calls StartRecording().
/// </summary>
public record StartStandAloneRecordingCommand(
    Guid   AgentUserId,
    Guid   ApplicationId)
    : IRequest<StartStandAloneRecordingResponse>;

public class StartStandAloneRecordingResponse
{
    public Guid CollaborationId { get; init; }
    public Guid RecordingId     { get; init; }
    public string BlobPath      { get; init; } = string.Empty;
}
