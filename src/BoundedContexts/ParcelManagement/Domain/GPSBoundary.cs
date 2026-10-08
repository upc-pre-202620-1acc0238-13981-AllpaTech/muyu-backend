using System.Text.Json;

namespace Muyu.Modules.ParcelManagement.Domain;

/// <summary>Objeto de valor que representa el polígono geográfico de una parcela.</summary>
public sealed class GPSBoundary
{
    private GPSBoundary()
    {
    }

    private GPSBoundary(string geoJson, decimal areaHectares)
    {
        GeoJson = geoJson;
        AreaHectares = areaHectares;
    }

    public string GeoJson { get; private set; } = string.Empty;

    public decimal AreaHectares { get; private set; }

    public static GPSBoundary Create(string geoJson, decimal areaHectares)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(geoJson);

        if (areaHectares <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(areaHectares), "El área debe ser mayor que cero.");
        }

        using var document = JsonDocument.Parse(geoJson);
        var root = document.RootElement;

        if (!root.TryGetProperty("type", out var type) ||
            type.GetString() is not ("Polygon" or "MultiPolygon") ||
            !root.TryGetProperty("coordinates", out _))
        {
            throw new ArgumentException("El límite debe ser un GeoJSON Polygon o MultiPolygon válido.", nameof(geoJson));
        }

        return new GPSBoundary(geoJson, decimal.Round(areaHectares, 4));
    }
}
