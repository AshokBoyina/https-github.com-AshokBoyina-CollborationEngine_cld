namespace NICE.Platform.Collaboration.Application.Features.Messages.Queries.GetMessages;
using MediatR;
public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, IEnumerable<NICE.Platform.Collaboration.Contracts.Responses.ChatMessageResponse>>
{
    // TODO: inject repositories via constructor
    public Task<IEnumerable<NICE.Platform.Collaboration.Contracts.Responses.ChatMessageResponse>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        // TODO: fetch ordered messages for the collaboration
        throw new NotImplementedException();
    }
}
