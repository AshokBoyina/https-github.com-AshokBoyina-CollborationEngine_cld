namespace NICE.Platform.Collaboration.Application.Features.Applications.Commands.RegisterApplication;
using MediatR;
public record RegisterApplicationCommand(string Name, int MaxAgentsOnline, int MaxUsersOnline, int MaxCollaborationsPerAgent, string? WebhookUrl) : IRequest<Guid>;
