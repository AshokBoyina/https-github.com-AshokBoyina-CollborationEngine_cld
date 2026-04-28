namespace NICE.Platform.Collaboration.Application.Features.Users.Queries.GetAvailableAgents;
using MediatR;
public class GetAvailableAgentsQueryHandler : IRequestHandler<GetAvailableAgentsQuery, IEnumerable<NICE.Platform.Collaboration.Contracts.Responses.SessionResponse>>
{
    // TODO: inject repositories via constructor
    public Task<IEnumerable<NICE.Platform.Collaboration.Contracts.Responses.SessionResponse>> Handle(GetAvailableAgentsQuery request, CancellationToken cancellationToken)
    {
        // TODO: fetch available agents from session store
        throw new NotImplementedException();
    }
}
