namespace NICE.Platform.Collaboration.Application.Features.Messages.Commands.SendMessage;
using MediatR;
/// <param name="MessageType">Text | Whisper | System | Attachment</param>
public record SendMessageCommand(
    Guid    CollaborationId,
    Guid    SenderId,
    string  Content,
    Guid?   ReplyToId,
    string  MessageType = "Text")
    : IRequest<NICE.Platform.Collaboration.Contracts.Responses.ChatMessageResponse>;
