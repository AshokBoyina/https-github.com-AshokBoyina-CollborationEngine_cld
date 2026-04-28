namespace NICE.Platform.Collaboration.Application.Features.Applications.Commands.RegisterApplication;
using MediatR;
public class RegisterApplicationCommandHandler : IRequestHandler<RegisterApplicationCommand, Guid>
{
    // TODO: inject ICollaborationRepository, ISignalRNotifier, etc. via constructor
    public Task<Guid> Handle(RegisterApplicationCommand request, CancellationToken cancellationToken)
    {
        // TODO: hash API key, create ApplicationRegistration entity, persist
        throw new NotImplementedException();
    }
}
