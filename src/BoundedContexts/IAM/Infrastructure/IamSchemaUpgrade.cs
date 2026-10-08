using System.Data;
using Microsoft.EntityFrameworkCore;
using Muyu.Shared.Infrastructure;

namespace Muyu.Modules.IAM.Infrastructure;

/// <summary>
/// Actualiza las bases SQLite creadas por versiones anteriores que usaban EnsureCreated.
/// Se mantiene mientras el prototipo universitario migra a migraciones versionadas de EF Core.
/// </summary>
public sealed class IamSchemaUpgrade(MuyuDbContext dbContext)
{
    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        var columns = await GetUserColumnsAsync(cancellationToken);

        if (!columns.Contains("IdentityDocumentType"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE iam_users ADD COLUMN IdentityDocumentType TEXT NOT NULL DEFAULT 'Dni';",
                cancellationToken);
        }

        if (!columns.Contains("IdentityDocumentNumber"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE iam_users ADD COLUMN IdentityDocumentNumber TEXT NOT NULL DEFAULT '';",
                cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(
                "UPDATE iam_users SET IdentityDocumentNumber = printf('%08d', rowid) WHERE IdentityDocumentNumber = '';",
                cancellationToken);
        }

        if (!columns.Contains("Status"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE iam_users ADD COLUMN Status TEXT NOT NULL DEFAULT 'Active';",
                cancellationToken);
            if (columns.Contains("IsActive"))
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "UPDATE iam_users SET Status = CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END;",
                    cancellationToken);
            }
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_iam_users_IdentityDocumentNumber ON iam_users (IdentityDocumentNumber);",
            cancellationToken);
    }

    private async Task<HashSet<string>> GetUserColumnsAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State is not ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('iam_users');";

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }
}
