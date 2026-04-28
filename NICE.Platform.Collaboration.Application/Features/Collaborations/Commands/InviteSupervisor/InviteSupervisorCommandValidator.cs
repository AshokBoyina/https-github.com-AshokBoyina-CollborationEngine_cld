namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.InviteSupervisor;
using FluentValidation;
public class InviteSupervisorCommandValidator : AbstractValidator<InviteSupervisorCommand>
{
    public InviteSupervisorCommandValidator()
    {
        RuleFor(x => x.SupervisorId).NotEmpty();
        RuleFor(x => x.CollaborationId).NotEmpty();
    }
}
