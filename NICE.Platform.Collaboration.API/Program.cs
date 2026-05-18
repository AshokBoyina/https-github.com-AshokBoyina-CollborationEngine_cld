using NICE.Platform.Collaboration.Application;
using NICE.Platform.Collaboration.Infrastructure;
using NICE.Platform.Collaboration.API.Middleware;
using NICE.Platform.Collaboration.API.Hubs;
using NICE.Platform.Collaboration.API.Services;
using NICE.Platform.Collaboration.Application.Interfaces.Services;
using NICE.Platform.Collaboration.Contracts.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

// CORS — default policy (no name); app.UseCors() below picks this up automatically.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.SetIsOriginAllowed(origin =>
    {
        // Allow any localhost port in development; lock this down in production.
        var uri = new Uri(origin);
        return uri.Host is "localhost" or "127.0.0.1";
    })
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "NICE Platform Collaboration API",
        Version     = "v1",
        Description = "Real-time collaboration engine — authenticate via " +
                      "POST /api/v1/auth/validate before opening a SignalR connection."
    });

    const string apiKeyScheme = "X-Api-Key";
    options.AddSecurityDefinition(apiKeyScheme, new OpenApiSecurityScheme
    {
        Name        = "X-Api-Key",
        Type        = SecuritySchemeType.ApiKey,
        In          = ParameterLocation.Header,
        Description = "External provider JWT (READI / NICE). Required for POST /api/v1/auth/validate."
    });

    const string bearerScheme = "Bearer";
    options.AddSecurityDefinition(bearerScheme, new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Internal JWT issued after successful auth validation."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = bearerScheme
                }
            },
            []
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ── SignalR ────────────────────────────────────────────────────────────────
// Flip FeatureFlags:UseAzureSignalR = true when ready to scale out.
// Add Microsoft.Azure.SignalR to API.csproj first, then uncomment AddAzureSignalR().
var useAzureSignalR = builder.Configuration.GetValue<bool>("FeatureFlags:UseAzureSignalR");
if (useAzureSignalR)
{
    throw new InvalidOperationException(
        "UseAzureSignalR is true but Microsoft.Azure.SignalR is not yet referenced. " +
        "Add the NuGet package to NICE.Platform.Collaboration.API.csproj and " +
        "uncomment the AddAzureSignalR() call here.");
    // builder.Services.AddSignalR()
    //     .AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]!);
}
else
{
    builder.Services.AddSignalR(opts =>
    {
        // Increase limit to handle recording chunk metadata messages (well under 512KB)
        opts.MaximumReceiveMessageSize = 512 * 1024;  // 512 KB
    });
}

// ── ISignalRNotifier — registered here (not in Infrastructure DI) so that
//    Infrastructure.dll does NOT take a compile-time dep on CollaborationHub.
builder.Services.AddScoped<ISignalRNotifier, SignalRNotifier>();

// ── JWT bearer auth ────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtConfig = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = jwtConfig["Issuer"],
            ValidateAudience         = true,
            ValidAudience            = jwtConfig["Audience"],
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtConfig["Key"]!)),
            ClockSkew                = TimeSpan.FromMinutes(2)
        };

        // SignalR WebSocket connections pass the token as ?access_token=...
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

// ── Build ──────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Startup: clear stale CurrentSessions ──────────────────────────────────
// CurrentSessions rows are only valid while a SignalR connection is alive.
// Any row surviving a server restart belongs to a dead connection, so wipe them.
// UserSessions (history) is permanent and must NOT be cleared.
// NOTE: This runs BEFORE app.Run() so it executes at startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<NICE.Platform.Collaboration.Infrastructure.Persistence.CollaborationDbContext>();
    try
    {
        var stale = db.CurrentSessions.ToList();
        if (stale.Count > 0)
        {
            db.CurrentSessions.RemoveRange(stale);
            db.SaveChanges();
            app.Logger.LogInformation("Startup cleanup: removed {Count} stale CurrentSessions rows.", stale.Count);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Startup cleanup of CurrentSessions failed (non-fatal).");
    }
}

// ── Startup: ensure local storage folders exist ───────────────────────────
try
{
    var recordingsPath  = app.Configuration["LocalStorage:RecordingsPath"];
    var attachmentsPath = app.Configuration["LocalStorage:AttachmentsPath"];
    if (!string.IsNullOrWhiteSpace(recordingsPath))  Directory.CreateDirectory(recordingsPath);
    if (!string.IsNullOrWhiteSpace(attachmentsPath)) Directory.CreateDirectory(attachmentsPath);
    app.Logger.LogInformation("Local storage folders ready: Recordings={R} Attachments={A}",
        recordingsPath, attachmentsPath);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Could not create local storage folders (non-fatal).");
}

// ── Middleware pipeline ────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NICE Collaboration API v1");
        c.RoutePrefix = string.Empty;   // Serve Swagger UI at root /
    });
}

app.UseCors();                                      // uses the default policy registered above
app.UseMiddleware<GlobalExceptionMiddleware>();      // unhandled-exception → JSON 500
app.UseMiddleware<ApiKeyMiddleware>();               // validates X-Api-Key header
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// ── SignalR hub routes ────────────────────────────────────────────────────
app.MapHub<CollaborationHub>(HubRoutes.Collaboration);
app.MapHub<RecordingHub>(HubRoutes.Recording);

app.Run();

// Required so WebApplicationFactory<Program> in Tests project can access the entry-point type.
public partial class Program { }
