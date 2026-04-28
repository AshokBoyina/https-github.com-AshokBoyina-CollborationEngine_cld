namespace NICE.Platform.Collaboration.Application.Features.Messages.Commands.SendMessage;
using FluentValidation;
public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.CollaborationId).NotEmpty();
    }
}
