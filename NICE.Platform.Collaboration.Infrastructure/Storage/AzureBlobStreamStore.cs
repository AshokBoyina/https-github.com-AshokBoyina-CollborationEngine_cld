namespace NICE.Platform.Collaboration.Infrastructure.Storage;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using NICE.Platform.Collaboration.Application.Interfaces.Services;

/// <summary>
/// Streams recording chunks to Azure Blob Storage using AppendBlobClient.
/// Each chunk becomes one append-block — the blob grows in real-time.
/// Pair with LocalDiskStreamStore via DualRecordingStreamStore so you get
/// both local backup and cloud redundancy.
/// </summary>
public sealed class AzureBlobStreamStore(
    BlobServiceClient              blobService,
    string                         containerName,
    ILogger<AzureBlobStreamStore>  logger) : IRecordingStreamStore
{
    private BlobContainerClient Container =>
        blobService.GetBlobContainerClient(containerName);

    public async Task<string> InitAsync(Guid recordingId, CancellationToken ct = default)
    {
        await Container.CreateIfNotExistsAsync(cancellationToken: ct);
        var blobName = $"recordings/{recordingId}.webm";
        var client   = Container.GetAppendBlobClient(blobName);
        await client.CreateIfNotExistsAsync(cancellationToken: ct);
        logger.LogInformation("AzureBlobStream: created append blob {Blob}", blobName);
        return blobName;
    }

    public async Task AppendChunkAsync(Guid recordingId, byte[] chunk, CancellationToken ct = default)
    {
        var blobName = $"recordings/{recordingId}.webm";
        var client   = Container.GetAppendBlobClient(blobName);

        // Azure append blocks have a max of 4 MB each — our chunks are ≤ 200 KB, well within limit
        using var ms = new MemoryStream(chunk);
        await client.AppendBlockAsync(ms, cancellationToken: ct);
    }

    public async Task<long> FinalizeAsync(Guid recordingId, CancellationToken ct = default)
    {
        var blobName = $"recordings/{recordingId}.webm";
        var client   = Container.GetAppendBlobClient(blobName);
        var props    = await client.GetPropertiesAsync(cancellationToken: ct);
        logger.LogInformation("AzureBlobStream: finalized {Blob} ({Bytes} bytes)",
            blobName, props.Value.ContentLength);
        return props.Value.ContentLength;
    }

    // Azure blobs have no local path
    public string? GetLocalPath(Guid recordingId) => null;
}
