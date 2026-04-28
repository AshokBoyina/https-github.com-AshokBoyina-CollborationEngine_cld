namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.TransferCollaboration;
using MediatR;
public record TransferCollaborationCommand(Guid CollaborationId, Guid FromAgentId, Guid ToAgentId, string? Reason) : IRequest<Unit>;
