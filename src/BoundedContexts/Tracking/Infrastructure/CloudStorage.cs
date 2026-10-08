using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Muyu.Modules.Tracking.Application;

namespace Muyu.Modules.Tracking.Infrastructure;

public sealed class CloudStorage(IConfiguration configuration) : ICloudStorage
{
    public async Task<StoredFile> StoreAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var root = configuration["Storage:EvidencePath"];
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(AppContext.BaseDirectory, "evidence-storage");
        }

        Directory.CreateDirectory(root);
        var safeName = Path.GetFileName(fileName);
        var key = $"{Guid.NewGuid():N}-{safeName}";
        var fullPath = Path.Combine(root, key);

        await using var output = File.Create(fullPath);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long size = 0;
        int bytesRead;

        while ((bytesRead = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            hash.AppendData(buffer, 0, bytesRead);
            size += bytesRead;
        }

        var sha256 = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        return new StoredFile(new Uri(fullPath).AbsoluteUri, sha256, size);
    }
}
