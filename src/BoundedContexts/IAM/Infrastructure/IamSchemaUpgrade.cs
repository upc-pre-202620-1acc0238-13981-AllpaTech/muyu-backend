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

        // Las primeras versiones persistían IsActive como una columna NOT NULL.
        // El modelo actual usa Status e ignora IsActive, por lo que SQLite rechaza
        // cualquier INSERT si conservamos la tabla antigua. SQLite no permite
        // eliminar columnas con restricciones de forma portable; reconstruimos la
        // tabla conservando sus datos y traduciendo el estado anterior.
        if (columns.Contains("IsActive"))
        {
            await RebuildLegacyUsersTableAsync(cancellationToken);
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_iam_users_IdentityDocumentNumber ON iam_users (IdentityDocumentNumber);",
            cancellationToken);
    }

    private async Task RebuildLegacyUsersTableAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "PRAGMA foreign_keys = OFF;",
            cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE iam_users_upgrade (
                    Id TEXT NOT NULL CONSTRAINT PK_iam_users PRIMARY KEY,
                    Email TEXT NOT NULL,
                    DisplayName TEXT NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    RoleId TEXT NOT NULL,
                    CreatedAtUtc TEXT NOT NULL,
                    UpdatedAtUtc TEXT NULL,
                    IdentityDocumentType TEXT NOT NULL,
                    IdentityDocumentNumber TEXT NOT NULL,
                    CONSTRAINT FK_iam_users_iam_roles_RoleId
                        FOREIGN KEY (RoleId) REFERENCES iam_roles (Id) ON DELETE RESTRICT
                );
                """,
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO iam_users_upgrade (
                    Id,
                    Email,
                    DisplayName,
                    PasswordHash,
                    Status,
                    RoleId,
                    CreatedAtUtc,
                    UpdatedAtUtc,
                    IdentityDocumentType,
                    IdentityDocumentNumber)
                SELECT
                    Id,
                    Email,
                    DisplayName,
                    PasswordHash,
                    CASE
                        WHEN Status IS NULL OR trim(Status) = ''
                            THEN CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END
                        ELSE Status
                    END,
                    RoleId,
                    CreatedAtUtc,
                    UpdatedAtUtc,
                    IdentityDocumentType,
                    IdentityDocumentNumber
                FROM iam_users;
                """,
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                "DROP TABLE iam_users;",
                cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE iam_users_upgrade RENAME TO iam_users;",
                cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IX_iam_users_Email ON iam_users (Email);",
                cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(
                "CREATE INDEX IX_iam_users_RoleId ON iam_users (RoleId);",
                cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IX_iam_users_IdentityDocumentNumber ON iam_users (IdentityDocumentNumber);",
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "PRAGMA foreign_keys = ON;",
                cancellationToken);
        }
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
