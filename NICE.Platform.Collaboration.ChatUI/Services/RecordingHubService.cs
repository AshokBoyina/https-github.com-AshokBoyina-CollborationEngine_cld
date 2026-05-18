namespace NICE.Platform.Collaboration.ChatUI.Services;

using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using NICE.Platform.Collaboration.ChatUI.Models;

using SignalRState = Microsoft.AspNetCore.SignalR.Client.HubConnectionState;
using ChatState    = NICE.Platform.Collaboration.ChatUI.Models.HubConnectionState;

public sealed class RecordingHubService(IAuthService auth, HttpClient http) : IRecordingHubService
{
    private HubConnection? _hub;

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    public ChatState ConnectionState =>
        _hub?.State switch
        {
            SignalRState.Connected    => ChatState.Connected,
            SignalRState.Connecting   => ChatState.Connecting,
            SignalRState.Reconnecting => ChatState.Reconnecting,
            _                        => ChatState.Disconnected
        };

    // ── Events ──────────────────────────────────────────────────────────────
    public event Action<LiveRecordingInfo>?       OnRecordingStarted;
    public event Action<string>?                  OnRecordingStopped;
    public event Action<string, int, int>?        OnRecordingChunk;
    public event Action<RecordingWhisper>?        OnRecordingWhisper;
    public event Action<List<LiveRecordingInfo>>? OnLiveSnapshot;
    public event Action<string>?                  OnForceDisconnect;
    public event Action?                          OnStateChanged;

    // ── Connect / disconnect ─────────────────────────────────────────────────
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(auth.Current.Token))
            throw new InvalidOperationException(
                "Cannot connect to Recording hub: no session token. Log in first.");

        if (_hub?.State == SignalRState.Connected) return;

        var baseUrl = http.BaseAddress!.ToString().TrimEnd('/');
        var hubUrl  = $"{baseUrl}/hubs/v1/recording";

        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl, opts =>
            {
                opts.AccessTokenProvider = () => Task.FromResult<string?>(auth.Current.Token);
            })
            .WithAutomaticReconnect()
            .Build();

        _hub.Reconnecting  += _ => { OnStateChanged?.Invoke(); return Task.CompletedTask; };
        _hub.Reconnected   += _ => { OnStateChanged?.Invoke(); return Task.CompletedTask; };
        _hub.Closed        += _ => { OnStateChanged?.Invoke(); return Task.CompletedTask; };

        // ── Server → client events ──────────────────────────────────────────
        _hub.On<JsonElement>("RecordingStarted", payload =>
        {
            var info = new LiveRecordingInfo
            {
                RecordingId      = payload.TryGetProperty("recordingId",     out var r) ? r.GetString() ?? "" : "",
                CollaborationId  = payload.TryGetProperty("collaborationId", out var c) ? c.GetString() ?? "" : "",
                AgentUserId      = payload.TryGetProperty("agentUserId",     out var u) ? u.GetString() ?? "" : "",
                AgentDisplayName = payload.TryGetProperty("agentName",       out var n) ? n.GetString() ?? "" : "",
                StartedAt        = payload.TryGetProperty("startedAt",       out var s) ? s.GetDateTime()  : DateTime.UtcNow
            };
            OnRecordingStarted?.Invoke(info);
        });

        _hub.On<JsonElement>("RecordingStopped", payload =>
        {
            var recId = payload.TryGetProperty("recordingId", out var r) ? r.GetString() ?? "" : "";
            OnRecordingStopped?.Invoke(recId);
        });

        _hub.On<JsonElement>("RecordingChunk", payload =>
        {
            var recId = payload.TryGetProperty("recordingId", out var r) ? r.GetString() ?? "" : "";
            var seq   = payload.TryGetProperty("sequence",    out var s) ? s.GetInt32()        : 0;
            var bytes = payload.TryGetProperty("sizeBytes",   out var b) ? b.GetInt32()        : 0;
            OnRecordingChunk?.Invoke(recId, seq, bytes);
        });

        _hub.On<JsonElement>("RecordingWhisper", payload =>
        {
            var whisper = new RecordingWhisper
            {
                RecordingId = payload.TryGetProperty("recordingId", out var r) ? r.GetString() ?? "" : "",
                Message     = payload.TryGetProperty("message",     out var m) ? m.GetString() ?? "" : "",
                FromName    = payload.TryGetProperty("fromName",    out var f) ? f.GetString() ?? "" : "",
                SentAt      = payload.TryGetProperty("sentAt",      out var t) ? t.GetDateTime()  : DateTime.UtcNow
            };
            OnRecordingWhisper?.Invoke(whisper);
        });

        _hub.On<JsonElement>("LiveRecordingSnapshot", payload =>
        {
            var list = new List<LiveRecordingInfo>();
            if (payload.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in payload.EnumerateArray())
                {
                    list.Add(new LiveRecordingInfo
                    {
                        RecordingId      = item.TryGetProperty("recordingId",     out var r) ? r.GetString() ?? "" : "",
                        CollaborationId  = item.TryGetProperty("collaborationId", out var c) ? c.GetString() ?? "" : "",
                        AgentUserId      = item.TryGetProperty("agentUserId",     out var u) ? u.GetString() ?? "" : "",
                        AgentDisplayName = item.TryGetProperty("agentDisplayName",out var n) ? n.GetString() ?? "" : "",
                        StartedAt        = item.TryGetProperty("startedAt",       out var s) ? s.GetDateTime()  : DateTime.UtcNow,
                        BytesWritten     = item.TryGetProperty("bytesWritten",    out var b) ? b.GetInt64()      : 0
                    });
                }
            }
            OnLiveSnapshot?.Invoke(list);
        });

        _hub.On<string>("ForceDisconnect", reason => OnForceDisconnect?.Invoke(reason));

        await _hub.StartAsync(ct);
        OnStateChanged?.Invoke();
    }

    public async Task DisconnectAsync()
    {
        if (_hub is null) return;
        await _hub.StopAsync();
        await _hub.DisposeAsync();
        _hub = null;
        OnStateChanged?.Invoke();
    }

    // ── Agent methods ────────────────────────────────────────────────────────
    public async Task<(string RecordingId, string CollaborationId)> StartRecordingAsync(string applicationId)
    {
        var tcs = new TaskCompletionSource<(string, string)>();
        using var sub = _hub!.On<JsonElement>("RecordingReady", payload =>
        {
            var recId  = payload.TryGetProperty("recordingId",     out var r) ? r.GetString() ?? "" : "";
            var collId = payload.TryGetProperty("collaborationId", out var c) ? c.GetString() ?? "" : "";
            tcs.TrySetResult((recId, collId));
        });
        await _hub!.InvokeAsync("StartRecording", applicationId);
        return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    public Task StopRecordingAsync(string recordingId)
        => _hub!.InvokeAsync("StopRecording", recordingId);

    // ── Supervisor methods ───────────────────────────────────────────────────
    public Task JoinStandAloneAsync(string applicationId)
        => _hub!.InvokeAsync("JoinStandAlone", applicationId);

    public Task WatchRecordingAsync(string recordingId)
        => _hub!.InvokeAsync("WatchRecording", recordingId);

    public Task StopWatchingAsync(string recordingId)
        => _hub!.InvokeAsync("StopWatching", recordingId);

    public Task WhisperToAgentAsync(string recordingId, string message)
        => _hub!.InvokeAsync("WhisperToAgent", recordingId, message);

    // ── Disposal ─────────────────────────────────────────────────────────────
    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            await _hub.DisposeAsync();
            _hub = null;
        }
    }
}
