using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NICE.Platform.Collaboration.ChatUI;
using NICE.Platform.Collaboration.ChatUI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── API base address (override in wwwroot/appsettings.json for production) ──
var apiBase = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBase) });

// ── Application services ────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICollaborationHubService, CollaborationHubService>();
builder.Services.AddScoped<IRecordingHubService, RecordingHubService>();
builder.Services.AddScoped<IDemoApiService, DemoApiService>();

await builder.Build().RunAsync();
