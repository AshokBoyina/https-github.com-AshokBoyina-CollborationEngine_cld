namespace NICE.Platform.Collaboration.Infrastructure.Features.Applications.Commands.RegisterApplication;

using MediatR;
using NICE.Platform.Collaboration.Application.Features.Applications.Commands.RegisterApplication;
using Microsoft.Extensions.Logging;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

public sealed class RegisterApplicationCommandHandler(
    CollaborationDbContext db,
    ILogger<RegisterApplicationCommandHandler> logger)
    : IRequestHandler<RegisterApplicationCommand, Guid>
{
    public async Task<Guid> Handle(
        RegisterApplicationCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("RegisterApplication: name={Name}", request.Name);

        // Generate a new API key and hash it for storage
        var rawApiKey    = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hashedApiKey = HashApiKey(rawApiKey);

        var app = new CollaborationApplication
        {
            Id                = Guid.NewGuid(),
            Name              = request.Name,
            HashedApiKey      = hashedApiKey,
            AuthProvider      = "NICE",                    // default; can be updated later
            MaxAgentsOnline   = request.MaxAgentsOnline,
            MaxUsersOnline    = request.MaxUsersOnline,
            BlobContainerPath = $"apps/{Guid.NewGuid()}",
            WebhookUrl        = request.WebhookUrl,
            IsActive          = true,
            CreatedAt         = DateTime.UtcNow
        };

        await db.Applications.AddAsync(app, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Application registered: id={AppId} name={Name}", app.Id, app.Name);

        return app.Id;
    }

    private static string HashApiKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
