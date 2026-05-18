namespace NICE.Platform.Collaboration.Infrastructure.Storage;

using NICE.Platform.Collaboration.Application.Interfaces.Services;

/// <summary>
/// Writes to both local disk AND Azure Blob in parallel.
/// Used when FeatureFlags:UseAzureBlob = true so we keep a local copy for
/// fast supervisor streaming while the blob serves as cloud backup.
/// </summary>
public sealed class DualRecordingStreamStore(
    LocalDiskStreamStore  local,
    AzureBlobStreamStore  azure) : IRecordingStreamStore
{
    public async Task<string> InitAsync(Guid recordingId, CancellationToken ct = default)
    {
        // Run both in parallel; return the blob URI (azure path)
        var (localPath, blobPath) = await (
            local.InitAsync(recordingId, ct),
            azure.InitAsync(recordingId, ct)).WhenAll();
        return blobPath;
    }

    public Task AppendChunkAsync(Guid recordingId, byte[] chunk, CancellationToken ct = default)
        => Task.WhenAll(
            local.AppendChunkAsync(recordingId, chunk, ct),
            azure.AppendChunkAsync(recordingId, chunk, ct));

    public async Task<long> FinalizeAsync(Guid recordingId, CancellationToken ct = default)
    {
        var (localBytes, _) = await (
            local.FinalizeAsync(recordingId, ct),
            azure.FinalizeAsync(recordingId, ct)).WhenAll();
        return localBytes;          // local size is authoritative for DB
    }

    public string? GetLocalPath(Guid recordingId) => local.GetLocalPath(recordingId);
}

// ── ValueTuple extension so WhenAll works on two Tasks<T> ──────────────────
file static class TaskExtensions
{
    public static async Task<(T1, T2)> WhenAll<T1, T2>(this (Task<T1>, Task<T2>) tasks)
    {
        await Task.WhenAll(tasks.Item1, tasks.Item2);
        return (tasks.Item1.Result, tasks.Item2.Result);
    }
}
