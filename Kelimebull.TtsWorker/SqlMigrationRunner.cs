using Microsoft.Data.SqlClient;

namespace Kelimebull.TtsWorker;

public sealed class SqlMigrationRunner
{
    private readonly IConfiguration _configuration;

    public SqlMigrationRunner(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("umbracoDbDSN")
            ?? throw new InvalidOperationException("Connection string 'umbracoDbDSN' is required.");

        var sqlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Kelimebull.Tts.Core", "Sql", "001_create_tts_tables.sql"));
        if (!File.Exists(sqlPath))
        {
            sqlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Sql", "001_create_tts_tables.sql"));
        }

        var sql = await File.ReadAllTextAsync(sqlPath, cancellationToken);
        var batches = sql.Split(new[] { "\r\nGO\r\n", "\nGO\n", "\r\nGO\n" }, StringSplitOptions.RemoveEmptyEntries);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await using var command = new SqlCommand(batch, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
