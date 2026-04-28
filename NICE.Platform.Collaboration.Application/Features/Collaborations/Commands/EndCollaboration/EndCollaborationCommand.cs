namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.EndCollaboration;
using MediatR;
public record EndCollaborationCommand(Guid CollaborationId, Guid RequestedBy) : IRequest<Unit>;
