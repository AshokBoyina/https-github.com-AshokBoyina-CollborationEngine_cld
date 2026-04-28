namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.SupervisorJoin;
using FluentValidation;
public class SupervisorJoinCommandValidator : AbstractValidator<SupervisorJoinCommand>
{
    public SupervisorJoinCommandValidator()
    {
        RuleFor(x => x.CollaborationId).NotEmpty();
    }
}
