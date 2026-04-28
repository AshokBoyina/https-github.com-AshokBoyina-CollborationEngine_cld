namespace NICE.Platform.Collaboration.Application.Features.Messages.Commands.SendMessage;
using MediatR;
public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, NICE.Platform.Collaboration.Contracts.Responses.ChatMessageResponse>
{
    // TODO: inject ICollaborationRepository, ISignalRNotifier, etc. via constructor
    public Task<NICE.Platform.Collaboration.Contracts.Responses.ChatMessageResponse> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        // TODO: get next sequence number, persist, broadcast to group and silent-monitor group
        throw new NotImplementedException();
    }
}
