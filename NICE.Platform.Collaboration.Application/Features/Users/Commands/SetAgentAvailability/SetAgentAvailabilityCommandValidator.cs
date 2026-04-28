namespace NICE.Platform.Collaboration.Application.Features.Users.Commands.SetAgentAvailability;
using FluentValidation;
public class SetAgentAvailabilityCommandValidator : AbstractValidator<SetAgentAvailabilityCommand>
{
    public SetAgentAvailabilityCommandValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
    }
}
