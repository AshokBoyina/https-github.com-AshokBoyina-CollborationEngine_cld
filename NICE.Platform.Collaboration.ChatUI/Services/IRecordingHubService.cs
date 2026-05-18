namespace NICE.Platform.Collaboration.ChatUI.Services;

using NICE.Platform.Collaboration.ChatUI.Models;

/// <summary>
/// Client-side SignalR connection to the Recording hub.
/// Used by both agents (recording) and StandAlone supervisors (monitoring).
/// </summary>
public interface IRecordingHubService : IAsyncDisposable
{
    HubConnectionState ConnectionState { get; }

    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync();

    // ── Agent methods ───────────────────────────────────────────────────────
    Task<(string RecordingId, string CollaborationId)> StartRecordingAsync(string applicationId);
    Task StopRecordingAsync(string recordingId);

    // ── Supervisor methods ──────────────────────────────────────────────────
    Task JoinStandAloneAsync(string applicationId);
    Task WatchRecordingAsync(string recordingId);
    Task StopWatchingAsync(string recordingId);
    Task WhisperToAgentAsync(string recordingId, string message);

    // ── Events pushed from server ────────────────────────────────────────────
    event Action<LiveRecordingInfo>? OnRecordingStarted;
    event Action<string>?            OnRecordingStopped;   // recordingId
    event Action<string, int, int>?  OnRecordingChunk;     // recordingId, sequence, sizeBytes
    event Action<RecordingWhisper>?  OnRecordingWhisper;   // whisper received (agent side)
    event Action<List<LiveRecordingInfo>>? OnLiveSnapshot; // initial snapshot on supervisor connect
    event Action<string>?            OnForceDisconnect;
    event Action?                    OnStateChanged;
}

public class LiveRecordingInfo
{
    public string   RecordingId      { get; set; } = "";
    public string   CollaborationId  { get; set; } = "";
    public string   AgentUserId      { get; set; } = "";
    public string   AgentDisplayName { get; set; } = "";
    public DateTime StartedAt        { get; set; }
    public long     BytesWritten     { get; set; }
    // computed
    public string DurationLabel => (DateTime.UtcNow - StartedAt) switch
    {
        var d when d.TotalHours   >= 1  => $"{(int)d.TotalHours}h {d.Minutes:D2}m",
        var d when d.TotalMinutes >= 1  => $"{(int)d.TotalMinutes}m {d.Seconds:D2}s",
        var d                           => $"{d.Seconds}s"
    };
    public string FileSizeFmt => BytesWritten switch
    {
        >= 1_073_741_824 => $"{BytesWritten / 1_073_741_824.0:F1} GB",
        >= 1_048_576     => $"{BytesWritten / 1_048_576.0:F1} MB",
        >= 1_024         => $"{BytesWritten / 1_024.0:F1} KB",
        _                => $"{BytesWritten} B"
    };
}

public class RecordingWhisper
{
    public string   RecordingId { get; set; } = "";
    public string   Message     { get; set; } = "";
    public string   FromName    { get; set; } = "";
    public DateTime SentAt      { get; set; }
}
