namespace NICE.Platform.Collaboration.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Azure.Storage.Blobs;
using NICE.Platform.Collaboration.Application.Auth;
using NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Application.Interfaces.Services;
using NICE.Platform.Collaboration.Application.Interfaces.Auth;
using NICE.Platform.Collaboration.Infrastructure.Auth;
using NICE.Platform.Collaboration.Infrastructure.Auth.Settings;
using NICE.Platform.Collaboration.Infrastructure.Auth.Validators;
using NICE.Platform.Collaboration.Infrastructure.Persistence;
using NICE.Platform.Collaboration.Infrastructure.Persistence.Repositories;
using NICE.Platform.Collaboration.Infrastructure.Session;
using NICE.Platform.Collaboration.Infrastructure.Storage;
using NICE.Platform.Collaboration.Infrastructure.Bot;
using NICE.Platform.Collaboration.Infrastructure.WebRTC;
using NICE.Platform.Collaboration.Infrastructure.Webhooks;
using NICE.Platform.Collaboration.Infrastructure.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration config)
    {
        // ── Auth validation settings (mock switch + provider URLs) ─────────────
        services.Configure<AuthValidationSettings>(
            config.GetSection(AuthValidationSettings.SectionName));

        // ── Pluggable validators ───────────────────────────────────────────────
        // READI and NICE validators use typed HttpClients so each gets its own
        // HttpMessageHandler pool (connection pooling, DNS refresh).
        services.AddHttpClient<ReadiAuthValidator>();
        services.AddHttpClient<NiceAuthValidator>();

        // Anonymous validator makes no HTTP calls — plain singleton is fine.
        services.AddSingleton<AnonymousAuthValidator>();

        // Factory — selects the right validator from the DI container.
        services.AddSingleton<IAuthValidatorFactory, AuthValidatorFactory>();

        // ── Application config provider (JSON mock → SQL in Phase 2) ─────────
        // Singleton: reads from IConfiguration which is already a singleton.
        services.AddSingleton<IApplicationConfigProvider, JsonApplicationConfigProvider>();

        // ── Internal JWT token service ─────────────────────────────────────────
        services.AddScoped<ITokenService, JwtTokenService>();

        // ── Legacy multi-provider JWT auth service (kept for backwards compatibility) ──
        services.Configure<Dictionary<string, AuthProviderConfig>>(
            config.GetSection("AuthProviders"));
        services.AddSingleton<IExternalAuthService, MultiProviderJwtAuthService>();

        // ── EF Core — SQL Server ───────────────────────────────────────────────
        services.AddDbContext<CollaborationDbContext>(opts =>
            opts.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // ── Redis ──────────────────────────────────────────────────────────────
        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(config.GetConnectionString("Redis")!));

        // ── Azure Blob Storage ─────────────────────────────────────────────────
        var blobConn  = config["Azure:BlobStorage:ConnectionString"]!;
        var container = config["Azure:BlobStorage:ContainerName"] ?? "collaborations";
        services.AddSingleton(_ => new BlobServiceClient(blobConn));
        services.AddScoped<IBlobStorageService>(sp =>
            new AzureBlobStorageService(sp.GetRequiredService<BlobServiceClient>(), container));

        // ── Repositories ───────────────────────────────────────────────────────
        services.AddScoped<IApplicationRepository,  ApplicationRepository>();
        services.AddScoped<IUserRepository,          UserRepository>();
        services.AddScoped<ICollaborationRepository, CollaborationRepository>();
        services.AddScoped<IChatMessageRepository,   ChatMessageRepository>();
        services.AddScoped<IRecordingRepository,     RecordingRepository>();
        services.AddScoped<ITransferRepository,      TransferRepository>();

        // ── Services ───────────────────────────────────────────────────────────
        services.AddScoped<ISessionStore,      RedisSessionStore>();
        services.AddScoped<ISignalRNotifier,   SignalRNotifier>();
        services.AddScoped<IBotService,        AzureAIFoundryBotService>();
        services.AddScoped<IIceServerProvider, TurnStunProvider>();
        services.AddHttpClient<IWebhookDispatcher, HttpWebhookDispatcher>();

        return services;
    }
}
