namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Queries.GetCollaborationById;
using MediatR;
public class GetCollaborationByIdQueryHandler : IRequestHandler<GetCollaborationByIdQuery, NICE.Platform.Collaboration.Contracts.Responses.CollaborationResponse?>
{
    // TODO: inject repositories via constructor
    public Task<NICE.Platform.Collaboration.Contracts.Responses.CollaborationResponse?> Handle(GetCollaborationByIdQuery request, CancellationToken cancellationToken)
    {
        // TODO: fetch from repository and map to response
        throw new NotImplementedException();
    }
}
