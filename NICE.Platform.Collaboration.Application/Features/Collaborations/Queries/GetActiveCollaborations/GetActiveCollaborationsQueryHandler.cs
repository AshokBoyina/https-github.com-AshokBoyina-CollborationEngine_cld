namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Queries.GetActiveCollaborations;
using MediatR;
public class GetActiveCollaborationsQueryHandler : IRequestHandler<GetActiveCollaborationsQuery, IEnumerable<NICE.Platform.Collaboration.Contracts.Responses.CollaborationResponse>>
{
    // TODO: inject repositories via constructor
    public Task<IEnumerable<NICE.Platform.Collaboration.Contracts.Responses.CollaborationResponse>> Handle(GetActiveCollaborationsQuery request, CancellationToken cancellationToken)
    {
        // TODO: fetch active collaborations for the application
        throw new NotImplementedException();
    }
}
