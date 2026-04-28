using NICE.Platform.Collaboration.Application;
using NICE.Platform.Collaboration.Infrastructure;
using NICE.Platform.Collaboration.API.Middleware;
using NICE.Platform.Collaboration.API.Hubs;
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "NICE Platform Collaboration API",
        Version     = "v1",
        Description = "Real-time collaboration engine — authenticate via POST /api/auth/validate " +
                      "before opening a SignalR connection."
    });

    // ── Security scheme 1: X-Api-Key header (auth pre-flight endpoint) ────
    const string apiKeyScheme = "X-Api-Key";
    options.AddSecurityDefinition(apiKeyScheme, new OpenApiSecurityScheme
    {
        Name        = "X-Api-Key",
        Type        = SecuritySchemeType.ApiKey,
        In          = ParameterLocation.Header,
        Description = "External provider JWT (Readi / Nice). " +
                      "Required only for POST /api/auth/validate."
    });

    // ── Security scheme 2: Bearer JWT (all other protected endpoints) ─────
    const string bearerScheme = "Bearer";
    options.AddSecurityDefinition(bearerScheme, new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Internal JWT issued after successful auth validation. " +
                       "Enter the token only — the 'Bearer ' prefix is added automatically."
    });

    // Apply Bearer globally so the padlock icon appears on all protected operations.
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

    // Include XML doc comments from this assembly.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.AddSignalR();
// Uncomment to use Azure SignalR Service:
// builder.Services.AddSignalR().AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]);

// Internal JWT bearer — used by all endpoints AFTER the auth pre-flight.
// External provider tokens are validated separately in AuthController via IExternalAuthService.
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

        // Allow SignalR to pass the token via query string (?access_token=...)
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

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

// Swagger available in all environments.
// Lock this down behind a reverse-proxy auth rule in production if needed.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "NICE Platform Collaboration API v1");
    options.RoutePrefix = "swagger";   // UI at /swagger
    options.DisplayRequestDuration();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CollaborationHub>(HubRoutes.Collaboration);
app.MapHub<RecordingHub>(HubRoutes.Recording);
app.MapHealthChecks("/health");

app.Run();

// Expose Program for integration tests
public partial class Program { }
