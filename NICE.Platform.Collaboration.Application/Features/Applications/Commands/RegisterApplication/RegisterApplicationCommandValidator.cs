namespace NICE.Platform.Collaboration.Application.Features.Applications.Commands.RegisterApplication;
using FluentValidation;
public class RegisterApplicationCommandValidator : AbstractValidator<RegisterApplicationCommand>
{
    public RegisterApplicationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MaxAgentsOnline).GreaterThan(0);
        RuleFor(x => x.MaxUsersOnline).GreaterThan(0);
        RuleFor(x => x.MaxCollaborationsPerAgent).GreaterThan(0);
    }
}
