namespace NICE.Platform.Collaboration.Application.Features.Messages.Queries.GetMessages;
using MediatR;
public record GetMessagesQuery(Guid CollaborationId) : IRequest<IEnumerable<NICE.Platform.Collaboration.Contracts.Responses.ChatMessageResponse>>;
