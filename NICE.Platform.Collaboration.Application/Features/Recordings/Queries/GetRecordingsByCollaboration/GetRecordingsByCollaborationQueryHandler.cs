namespace NICE.Platform.Collaboration.Application.Features.Recordings.Queries.GetRecordingsByCollaboration;
using MediatR;
public class GetRecordingsByCollaborationQueryHandler : IRequestHandler<GetRecordingsByCollaborationQuery, IEnumerable<NICE.Platform.Collaboration.Contracts.Responses.RecordingResponse>>
{
    // TODO: inject repositories via constructor
    public Task<IEnumerable<NICE.Platform.Collaboration.Contracts.Responses.RecordingResponse>> Handle(GetRecordingsByCollaborationQuery request, CancellationToken cancellationToken)
    {
        // TODO: fetch recordings and map to response
        throw new NotImplementedException();
    }
}
