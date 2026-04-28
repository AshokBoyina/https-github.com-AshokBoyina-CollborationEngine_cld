namespace NICE.Platform.Collaboration.Application.Features.Users.Queries.GetAvailableSupervisors;
using MediatR;
public class GetAvailableSupervisorsQueryHandler : IRequestHandler<GetAvailableSupervisorsQuery, IEnumerable<NICE.Platform.Collaboration.Contracts.Responses.SessionResponse>>
{
    // TODO: inject repositories via constructor
    public Task<IEnumerable<NICE.Platform.Collaboration.Contracts.Responses.SessionResponse>> Handle(GetAvailableSupervisorsQuery request, CancellationToken cancellationToken)
    {
        // TODO: fetch available supervisors from session store
        throw new NotImplementedException();
    }
}
