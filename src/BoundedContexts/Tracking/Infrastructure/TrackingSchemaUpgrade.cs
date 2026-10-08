using System.Data;
using Microsoft.EntityFrameworkCore;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.Tracking.Infrastructure;

/// <summary>Actualiza las bases SQLite del prototipo previas a los metadatos GPS.</summary>
public sealed class TrackingSchemaUpgrade(MuyuDbContext dbContext)
{
    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        var columns = await GetEvidenceColumnsAsync(cancellationToken);

        if (!columns.Contains("Latitude"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE tracking_evidences ADD COLUMN Latitude REAL NOT NULL DEFAULT 0;",
                cancellationToken);
        }

        if (!columns.Contains("Longitude"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE tracking_evidences ADD COLUMN Longitude REAL NOT NULL DEFAULT 0;",
                cancellationToken);
        }
    }

    private async Task<HashSet<string>> GetEvidenceColumnsAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State is not ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('tracking_evidences');";

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }
}
