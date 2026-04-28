namespace NICE.Platform.Collaboration.Infrastructure.Storage;
using NICE.Platform.Collaboration.Application.Interfaces.Services;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
public class AzureBlobStorageService(BlobServiceClient client, string containerName) : IBlobStorageService
{
    public async Task<string> UploadAsync(string blobPath, Stream content, string contentType, CancellationToken ct)
    {
        var container = client.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);
        var blob = container.GetBlobClient(blobPath);
        await blob.UploadAsync(content, overwrite: true, cancellationToken: ct);
        return blob.Uri.ToString();
    }
    public Task<string> GenerateSasUrlAsync(string blobPath, TimeSpan expiry, CancellationToken ct)
    {
        // TODO: generate SAS token with read permission
        throw new NotImplementedException();
    }
    public async Task DeleteAsync(string blobPath, CancellationToken ct)
    {
        var blob = client.GetBlobContainerClient(containerName).GetBlobClient(blobPath);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }
}
