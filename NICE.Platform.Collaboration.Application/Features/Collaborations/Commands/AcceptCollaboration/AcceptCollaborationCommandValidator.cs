namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.AcceptCollaboration;
using FluentValidation;
public class AcceptCollaborationCommandValidator : AbstractValidator<AcceptCollaborationCommand>
{
    public AcceptCollaborationCommandValidator()
    {
        RuleFor(x => x.CollaborationId).NotEmpty();
    }
}
