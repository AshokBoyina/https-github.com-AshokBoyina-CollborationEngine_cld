namespace NICE.Platform.Collaboration.Application.Features.Recordings.Queries.GetRecordingSasUrl;
using MediatR;
public class GetRecordingSasUrlQueryHandler : IRequestHandler<GetRecordingSasUrlQuery, string>
{
    // TODO: inject repositories via constructor
    public Task<string> Handle(GetRecordingSasUrlQuery request, CancellationToken cancellationToken)
    {
        // TODO: fetch recording blob path, generate SAS URL
        throw new NotImplementedException();
    }
}
