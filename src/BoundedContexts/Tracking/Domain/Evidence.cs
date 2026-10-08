using Muyu.Shared.Domain;

namespace Muyu.Modules.Tracking.Domain;

public sealed class Evidence : BaseEntity
{
    private Evidence()
    {
    }

    internal Evidence(
        string fileName,
        string contentType,
        string storageUri,
        string sha256,
        long sizeBytes,
        DateTimeOffset capturedAtUtc,
        string? note)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }

        FileName = fileName.Trim();
        ContentType = contentType.Trim().ToLowerInvariant();
        StorageUri = storageUri.Trim();
        Sha256 = sha256.Trim().ToLowerInvariant();
        SizeBytes = sizeBytes;
        CapturedAtUtc = capturedAtUtc;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public Guid MilestoneId { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public string StorageUri { get; private set; } = string.Empty;

    public string Sha256 { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public DateTimeOffset CapturedAtUtc { get; private set; }

    public string? Note { get; private set; }
}
