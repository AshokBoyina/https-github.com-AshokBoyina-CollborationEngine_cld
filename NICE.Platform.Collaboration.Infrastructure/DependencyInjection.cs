namespace NICE.Platform.Collaboration.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using NICE.Platform.Collaboration.Application.Auth;
using NICE.Platform.Collaboration.Application.Interfaces.Auth;
using NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Application.Interfaces.Services;
using NICE.Platform.Collaboration.Infrastructure.Auth;
using NICE.Platform.Collaboration.Infrastructure.Auth.Settings;
using NICE.Platform.Collaboration.Infrastructure.Auth.Validators;
using NICE.Platform.Collaboration.Infrastructure.Bot;
using NICE.Platform.Collaboration.Infrastructure.Persistence;
using NICE.Platform.Collaboration.Infrastructure.Persistence.Repositories;
using NICE.Platform.Collaboration.Infrastructure.Session;
using NICE.Platform.Collaboration.Infrastructure.Settings;
using NICE.Platform.Collaboration.Infrastructure.Storage;
using NICE.Platform.Collaboration.Infrastructure.WebRTC;
using NICE.Platform.Collaboration.Infrastructure.Webhooks;
using MediatR;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration config)
    {
        // ── MediatR — register handlers that live in this assembly (use CollaborationDbContext) ──
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // ── Feature flags ──────────────────────────────────────────────────────
        var flags = config
            .GetSection(FeatureFlagSettings.SectionName)
            .Get<FeatureFlagSettings>() ?? new FeatureFlagSettings();

        services.Configure<FeatureFlagSettings>(
            config.GetSection(FeatureFlagSettings.SectionName));

        // ── Auth validation settings (mock switch + provider URLs) ─────────────
        services.Configure<AuthValidationSettings>(
            config.GetSection(AuthValidationSettings.SectionName));

        // ── Pluggable auth validators ──────────────────────────────────────────
        services.AddHttpClient<ReadiAuthValidator>();
        services.AddHttpClient<NiceAuthValidator>();
        services.AddSingleton<AnonymousAuthValidator>();
        services.AddSingleton<IAuthValidatorFactory, AuthValidatorFactory>();

        // ── Application config provider (JSON mock → SQL in Phase 2) ──────────
        services.AddSingleton<IApplicationConfigProvider, JsonApplicationConfigProvider>();

        // ── JWT token service ──────────────────────────────────────────────────
        services.AddScoped<ITokenService, JwtTokenService>();

        // ── Legacy multi-provider JWT auth (kept for backwards compat) ─────────
        services.Configure<Dictionary<string, AuthProviderConfig>>(
            config.GetSection("AuthProviders"));
        services.AddSingleton<IExternalAuthService, MultiProviderJwtAuthService>();

        // ── EF Core — SQL Server ───────────────────────────────────────────────
        services.AddDbContext<CollaborationDbContext>(opts =>
            opts.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // ── Session store — SQL always (Redis permanently removed) ────────────
        services.AddScoped<ISessionStore, SqlSessionStore>();
        services.AddHostedService<SessionCacheCleanupService>();

        // ── In-memory recording session tracker (singleton) ───────────────────
        services.AddSingleton<IRecordingSessionTracker, RecordingSessionTracker>();

        // ── Blob / file storage ────────────────────────────────────────────────
        if (flags.UseAzureBlob)
        {
            var blobConn  = config["Azure:BlobStorage:ConnectionString"]!;
            var container = config["Azure:BlobStorage:ContainerName"] ?? "collaborations";
            services.AddSingleton(_ => new BlobServiceClient(blobConn));
            services.AddScoped<IBlobStorageService>(sp =>
                new AzureBlobStorageService(
                    sp.GetRequiredService<BlobServiceClient>(), container));

            // Dual stream store: local disk backup + Azure append blob
            services.AddSingleton<LocalDiskStreamStore>();
            services.AddSingleton(sp =>
                new AzureBlobStreamStore(
                    sp.GetRequiredService<BlobServiceClient>(), container,
                    sp.GetRequiredService<ILogger<AzureBlobStreamStore>>()));
            services.AddSingleton<IRecordingStreamStore>(sp =>
                new DualRecordingStreamStore(
                    sp.GetRequiredService<LocalDiskStreamStore>(),
                    sp.GetRequiredService<AzureBlobStreamStore>()));
        }
        else
        {
            services.AddScoped<IBlobStorageService, LocalDiskStorageService>();
            services.AddSingleton<IRecordingStreamStore, LocalDiskStreamStore>();
        }

        // ── ICE / STUN-TURN servers for WebRTC ────────────────────────────────
        if (flags.UseCustomTurn)
            services.AddSingleton<IIceServerProvider, TurnStunProvider>();
        else
            services.AddSingleton<IIceServerProvider, GoogleStunProvider>();

        // ── Repositories ───────────────────────────────────────────────────────
        services.AddScoped<IApplicationRepository,  ApplicationRepository>();
        services.AddScoped<IUserRepository,          UserRepository>();
        services.AddScoped<ICollaborationRepository, CollaborationRepository>();
        services.AddScoped<IMessageRepository,       MessageRepository>();
        services.AddScoped<IRecordingRepository,     RecordingRepository>();
        services.AddScoped<ITransferRepository,      TransferRepository>();

        // ── Bot service ────────────────────────────────────────────────────────
        // UseRealBot = false (default) → NoOpBotService; UI mock in ExternalChat.razor handles bot replies.
        // UseRealBot = true            → NiceBotApiService calls the real NICE bot API.
        // ISignalRNotifier registered in Program.cs (avoids Infrastructure→API circular dep).
        if (flags.UseRealBot)
            services.AddHttpClient<IBotService, NiceBotApiService>();
        else
            services.AddScoped<IBotService, NoOpBotService>();

        return services;
    }
}
