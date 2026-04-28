namespace NICE.Platform.Collaboration.Application.Features.Users.Commands.SetAgentAvailability;
using MediatR;
public record SetAgentAvailabilityCommand(Guid AgentId, Guid ApplicationId, string Status) : IRequest<Unit>;
