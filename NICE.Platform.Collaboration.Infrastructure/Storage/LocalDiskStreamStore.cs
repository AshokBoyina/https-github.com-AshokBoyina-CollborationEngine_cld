namespace NICE.Platform.Collaboration.Infrastructure.Storage;

using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NICE.Platform.Collaboration.Application.Interfaces.Services;

/// <summary>
/// Streams recording chunks to the local disk in real-time.
/// Each recording gets its own FileStream kept open during the session.
/// Files land in  LocalStorage:RecordingsPath / {recordingId}.webm
/// </summary>
public sealed class LocalDiskStreamStore(
    IConfiguration                   config,
    ILogger<LocalDiskStreamStore>    logger) : IRecordingStreamStore, IAsyncDisposable
{
    private string RootPath =>
        config["LocalStorage:RecordingsPath"]
        ?? Path.Combine(AppContext.BaseDirectory, "LocalStorage", "Recordings");

    // Open file streams keyed by recordingId
    private readonly ConcurrentDictionary<Guid, FileStream> _streams = new();

    public Task<string> InitAsync(Guid recordingId, CancellationToken ct = default)
    {
        Directory.CreateDirectory(RootPath);
        var path   = FilePath(recordingId);
        var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read,
                                   bufferSize: 65536, useAsync: true);
        _streams[recordingId] = stream;
        logger.LogInformation("RecordingStream: opened {Path}", path);
        return Task.FromResult($"recordings/{recordingId}.webm");
    }

    public async Task AppendChunkAsync(Guid recordingId, byte[] chunk, CancellationToken ct = default)
    {
        if (!_streams.TryGetValue(recordingId, out var stream))
        {
            logger.LogWarning("RecordingStream: no open stream for {Id} — chunk dropped", recordingId);
            return;
        }
        await stream.WriteAsync(chunk, ct);
        await stream.FlushAsync(ct);
    }

    public async Task<long> FinalizeAsync(Guid recordingId, CancellationToken ct = default)
    {
        if (!_streams.TryRemove(recordingId, out var stream))
            return 0;

        var length = stream.Length;
        await stream.FlushAsync(ct);
        await stream.DisposeAsync();
        logger.LogInformation("RecordingStream: finalized {Id} ({Bytes} bytes)", recordingId, length);
        return length;
    }

    public string? GetLocalPath(Guid recordingId) => FilePath(recordingId);

    private string FilePath(Guid id) => Path.Combine(RootPath, $"{id}.webm");

    public async ValueTask DisposeAsync()
    {
        foreach (var (_, stream) in _streams)
            await stream.DisposeAsync();
        _streams.Clear();
    }
}
