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
        double latitude,
        double longitude,
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

        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude));
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude));
        }

        FileName = fileName.Trim();
        ContentType = contentType.Trim().ToLowerInvariant();
        StorageUri = storageUri.Trim();
        Sha256 = sha256.Trim().ToLowerInvariant();
        SizeBytes = sizeBytes;
        CapturedAtUtc = capturedAtUtc;
        Latitude = latitude;
        Longitude = longitude;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public Guid MilestoneId { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public string StorageUri { get; private set; } = string.Empty;

    public string Sha256 { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public DateTimeOffset CapturedAtUtc { get; private set; }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public string? Note { get; private set; }
}
