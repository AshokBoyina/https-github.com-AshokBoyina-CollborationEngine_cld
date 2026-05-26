namespace NICE.Platform.Collaboration.Infrastructure.Features.Collaborations.Queries.GetActiveCollaborations;

using MediatR;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Queries.GetActiveCollaborations;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Core.Responses;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

public sealed class GetActiveCollaborationsQueryHandler(CollaborationDbContext db)
    : IRequestHandler<GetActiveCollaborationsQuery, IEnumerable<CollaborationResponse>>
{
    public async Task<IEnumerable<CollaborationResponse>> Handle(
        GetActiveCollaborationsQuery request, CancellationToken cancellationToken)
    {
        // Sessions older than 24 h with no EndedAt are abandoned (server restart, tab close, etc.).
        // Exclude them from the active list so the supervisor dashboard stays clean.
        var staleCutoff = DateTime.UtcNow.AddHours(-24);

        var collabs = await db.Collaborations
            .AsNoTracking()
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Where(c => c.ApplicationId == request.ApplicationId
                     && c.EndedAt == null          // not formally ended
                     && c.Status == "Active"        // both customer + agent are connected
                     && c.CreatedAt >= staleCutoff) // exclude sessions older than 24 h with no EndedAt
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return collabs.Select(c =>
        {
            // Customer — the External participant
            var customerParticipant = c.Participants
                .FirstOrDefault(p => p.UserType == "External");
            var customerName = customerParticipant?.User is { } cu
                ? $"{cu.FirstName} {cu.LastName}".Trim()
                : "Customer";

            // Agent — the first non-External participant (Agent / Supervisor / Internal)
            var agentParticipant = c.Participants
                .FirstOrDefault(p => p.UserType != "External");
            var agentName = agentParticipant?.User is { } au
                ? $"{au.FirstName} {au.LastName}".Trim()
                : "";


            return new CollaborationResponse
            {
                Id           = c.Id,
                Status       = c.Status,
                Type         = c.ChatMode,
                StartedAt    = c.CreatedAt,
                EndedAt      = c.EndedAt,
                CustomerName = customerName,
                AgentName    = agentName,
            };
        });
    }
}
