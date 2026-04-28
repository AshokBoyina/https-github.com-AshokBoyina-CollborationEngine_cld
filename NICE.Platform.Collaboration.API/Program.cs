using NICE.Platform.Collaboration.Application;
using NICE.Platform.Collaboration.Infrastructure;
using NICE.Platform.Collaboration.API.Middleware;
using NICE.Platform.Collaboration.API.Hubs;
using NICE.Platform.Collaboration.Contracts.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CollaborationHub>(HubRoutes.Collaboration);
app.MapHub<RecordingHub>(HubRoutes.Recording);
app.MapHealthChecks("/health");

app.Run();

// Expose Program for integration tests
public partial class Program { }
