namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.StartCollaboration;
using FluentValidation;
public class StartCollaborationCommandValidator : AbstractValidator<StartCollaborationCommand>
{
    public StartCollaborationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
