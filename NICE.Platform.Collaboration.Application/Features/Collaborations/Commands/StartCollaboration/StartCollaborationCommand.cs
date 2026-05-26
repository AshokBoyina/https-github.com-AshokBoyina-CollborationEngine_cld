namespace NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.StartCollaboration;

using MediatR;
using NICE.Platform.Collaboration.Core.Responses;

/// <param name="PreferredAgentId">Optional — null means "any available agent can accept".</param>
public record StartCollaborationCommand(Guid UserId, Guid? PreferredAgentId, Guid ApplicationId) : IRequest<CollaborationResponse>;
