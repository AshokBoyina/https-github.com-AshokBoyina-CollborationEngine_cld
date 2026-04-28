namespace NICE.Platform.Collaboration.Application.Features.Users.Commands.OnboardUser;
using MediatR;
public class OnboardUserCommandHandler : IRequestHandler<OnboardUserCommand, NICE.Platform.Collaboration.Contracts.Responses.SessionResponse>
{
    // TODO: inject ICollaborationRepository, ISignalRNotifier, etc. via constructor
    public Task<NICE.Platform.Collaboration.Contracts.Responses.SessionResponse> Handle(OnboardUserCommand request, CancellationToken cancellationToken)
    {
        // TODO: upsert UserProfile, assign role in ApplicationUser, create session token
        throw new NotImplementedException();
    }
}
