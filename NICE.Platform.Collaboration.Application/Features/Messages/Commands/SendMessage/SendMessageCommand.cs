namespace NICE.Platform.Collaboration.Application.Features.Messages.Commands.SendMessage;
using MediatR;
public record SendMessageCommand(Guid CollaborationId, Guid SenderId, string Content, Guid? ReplyToId) : IRequest<NICE.Platform.Collaboration.Contracts.Responses.ChatMessageResponse>;
