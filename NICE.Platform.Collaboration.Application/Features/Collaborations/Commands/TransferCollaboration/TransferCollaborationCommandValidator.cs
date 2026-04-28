namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.TransferCollaboration;
using FluentValidation;
public class TransferCollaborationCommandValidator : AbstractValidator<TransferCollaborationCommand>
{
    public TransferCollaborationCommandValidator()
    {
        RuleFor(x => x.ToAgentId).NotEmpty();
        RuleFor(x => x.FromAgentId).NotEqual(x => x.ToAgentId);
    }
}
