using System.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiTesourariaAMA.context;

/// <summary>
/// Adiciona os campos mensais em bancos SQLite locais que foram criados antes do uso do histórico EF.
/// A alteração é aditiva e preserva todos os lançamentos existentes.
/// </summary>
public static class DatabaseSchemaInitializer
{
    public static async Task EnsureDonationMonthAsync(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        var closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter) await connection.OpenAsync();

        try
        {
            await using var exists = connection.CreateCommand();
            exists.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'DebitosMembros';";
            var hasDebitsTable = Convert.ToInt32(await exists.ExecuteScalarAsync()) > 0;
            if (hasDebitsTable)
            {
                await using var columns = connection.CreateCommand();
                columns.CommandText = "PRAGMA table_info('DebitosMembros');";
                var hasMonth = false;
                await using (var reader = await columns.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        if (string.Equals(reader.GetString(1), "MesReferencia", StringComparison.OrdinalIgnoreCase))
                            hasMonth = true;
                }

                if (!hasMonth)
                {
                    await using var addColumn = connection.CreateCommand();
                    addColumn.CommandText = "ALTER TABLE DebitosMembros ADD COLUMN MesReferencia TEXT NULL;";
                    await addColumn.ExecuteNonQueryAsync();
                }

                await using var index = connection.CreateCommand();
                index.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS IX_DebitosMembros_UsuarioId_TipoDoacaoId_MesReferencia ON DebitosMembros (UsuarioId, TipoDoacaoId, MesReferencia) WHERE MesReferencia IS NOT NULL;";
                await index.ExecuteNonQueryAsync();
            }

            await using var typeTable = connection.CreateCommand();
            typeTable.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'TiposDoacao';";
            if (Convert.ToInt32(await typeTable.ExecuteScalarAsync()) > 0)
            {
                await using var typeColumns = connection.CreateCommand();
                typeColumns.CommandText = "PRAGMA table_info('TiposDoacao');";
                var hasOpeningMonth = false;
                await using (var reader = await typeColumns.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        if (string.Equals(reader.GetString(1), "MesAbertura", StringComparison.OrdinalIgnoreCase))
                            hasOpeningMonth = true;
                }

                if (!hasOpeningMonth)
                {
                    await using var addOpeningMonth = connection.CreateCommand();
                    addOpeningMonth.CommandText = "ALTER TABLE TiposDoacao ADD COLUMN MesAbertura TEXT NULL;";
                    await addOpeningMonth.ExecuteNonQueryAsync();
                }

                await EnsureColumnAsync(connection, "TiposDoacao", "ValorPadrao", "TEXT NOT NULL DEFAULT '0'");
                await EnsureColumnAsync(connection, "TiposDoacao", "TodosUsuarios", "INTEGER NOT NULL DEFAULT 1");

                await using var joinTable = connection.CreateCommand();
                joinTable.CommandText = """
                    CREATE TABLE IF NOT EXISTS TipoDoacaoUsuarios (
                        TipoDoacaoId INTEGER NOT NULL,
                        UsuarioId INTEGER NOT NULL,
                        PRIMARY KEY (TipoDoacaoId, UsuarioId),
                        FOREIGN KEY (TipoDoacaoId) REFERENCES TiposDoacao(Id) ON DELETE CASCADE,
                        FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_TipoDoacaoUsuarios_UsuarioId ON TipoDoacaoUsuarios (UsuarioId);
                    """;
                await joinTable.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            if (closeAfter) await connection.CloseAsync();
        }
    }

    private static async Task EnsureColumnAsync(System.Data.Common.DbConnection connection, string table, string column, string definition)
    {
        await using var columns = connection.CreateCommand();
        columns.CommandText = $"PRAGMA table_info('{table}');";
        var exists = false;
        await using (var reader = await columns.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase)) exists = true;
        }

        if (exists) return;
        await using var addColumn = connection.CreateCommand();
        addColumn.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
        await addColumn.ExecuteNonQueryAsync();
    }
}
