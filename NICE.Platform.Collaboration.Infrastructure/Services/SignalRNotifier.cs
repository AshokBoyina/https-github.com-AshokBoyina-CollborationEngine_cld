// ── MOVED ───────────────────────────────────────────────────────────────────
// SignalRNotifier was moved to NICE.Platform.Collaboration.API/Services/SignalRNotifier.cs
// so that Infrastructure does not take a dependency on the API hub type (CollaborationHub).
// Registration is now in Program.cs: services.AddScoped<ISignalRNotifier, SignalRNotifier>();
// ────────────────────────────────────────────────────────────────────────────
