using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Kelimebull.Tts.Core.Models;

namespace Kelimebull.Tts.Core.Data;

public sealed class TtsAudioRegistry : ITtsAudioRegistry
{
    private readonly string _connectionString;

    public TtsAudioRegistry(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("umbracoDbDSN")
            ?? throw new InvalidOperationException("Connection string 'umbracoDbDSN' is required for TTS.");
    }

    public async Task<TtsAudioRecord?> GetByHashAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT TOP (1)
                Id, ContentHash, OriginalText, NormalizedText, Language, Voice, Model, Format,
                PipelineVersion, SourceType, Status, CharacterCount, StorageKey, CdnUrl,
                OpenAiRequestId, ErrorMessage, CreatedUtc, UpdatedUtc, CompletedUtc
            FROM dbo.tts_audio_registry
            WHERE ContentHash = @ContentHash;
            """,
            connection);
        command.Parameters.AddWithValue("@ContentHash", contentHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRecord(reader) : null;
    }

    public async Task<TtsStatusSnapshot?> GetStatusSnapshotAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT TOP (1)
                r.Id, r.ContentHash, r.OriginalText, r.NormalizedText, r.Language, r.Voice, r.Model, r.Format,
                r.PipelineVersion, r.SourceType, r.Status, r.CharacterCount, r.StorageKey, r.CdnUrl,
                r.OpenAiRequestId, r.ErrorMessage, r.CreatedUtc, r.UpdatedUtc, r.CompletedUtc,
                q.Status AS QueueStatus,
                q.ErrorMessage AS QueueError
            FROM dbo.tts_audio_registry r
            LEFT JOIN dbo.tts_generation_queue q
                ON q.ContentHash = r.ContentHash AND q.Status <> 'completed'
            WHERE r.ContentHash = @ContentHash
            ORDER BY q.Id DESC;
            """,
            connection);
        command.Parameters.AddWithValue("@ContentHash", contentHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var registry = MapRecord(reader);
        var queueStatus = reader.IsDBNull(19) ? null : reader.GetString(19);
        var queueError = reader.IsDBNull(20) ? null : reader.GetString(20);
        return new TtsStatusSnapshot(registry, queueStatus, queueError);
    }

    public async Task<TtsAudioRecord> EnsureQueuedAsync(TtsAudioDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var record = await GetByHashAsync(connection, (SqlTransaction)transaction, descriptor.ContentHash, cancellationToken);
        if (record is null)
        {
            await InsertRegistryAsync(connection, (SqlTransaction)transaction, descriptor, cancellationToken);
        }
        else if (string.Equals(record.Status, "failed", StringComparison.OrdinalIgnoreCase))
        {
            // Daha önce başarısız olan kaydı, kullanıcı tekrar talep ettiğinde yeniden denemeye al.
            await ResetFailedRegistryAsync(connection, (SqlTransaction)transaction, descriptor.ContentHash, cancellationToken);
        }

        await ForceReleaseProcessingForUserRequestAsync(connection, (SqlTransaction)transaction, descriptor.ContentHash, cancellationToken);
        await EnqueueIfNeededAsync(connection, (SqlTransaction)transaction, descriptor, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByHashAsync(descriptor.ContentHash, cancellationToken)
            ?? throw new InvalidOperationException("TTS registry insert failed.");
    }

    public async Task<IReadOnlyList<TtsQueueItem>> ClaimPendingAsync(string workerId, int batchSize, TimeSpan lockDuration, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            ;WITH next_items AS
            (
                SELECT TOP (@BatchSize) q.Id
                FROM dbo.tts_generation_queue q WITH (UPDLOCK, READPAST, ROWLOCK)
                INNER JOIN dbo.tts_audio_registry r ON r.ContentHash = q.ContentHash
                WHERE (
                    (q.Status = 'pending' AND (q.NextAttemptUtc IS NULL OR q.NextAttemptUtc <= SYSUTCDATETIME()))
                    OR (q.Status = 'failed' AND q.NextAttemptUtc IS NOT NULL AND q.NextAttemptUtc <= SYSUTCDATETIME())
                    OR (q.Status = 'processing' AND q.LockedUntilUtc IS NOT NULL AND q.LockedUntilUtc <= SYSUTCDATETIME())
                  )
                  AND (q.LockedUntilUtc IS NULL OR q.LockedUntilUtc <= SYSUTCDATETIME())
                  AND r.Status <> 'completed'
                ORDER BY q.Priority DESC, q.CreatedUtc ASC
            )
            UPDATE q
               SET q.Status = 'processing',
                   q.WorkerId = @WorkerId,
                   q.LockedUntilUtc = DATEADD(second, @LockSeconds, SYSUTCDATETIME()),
                   q.AttemptCount = q.AttemptCount + 1,
                   q.UpdatedUtc = SYSUTCDATETIME()
            OUTPUT inserted.Id, inserted.ContentHash, r.OriginalText, r.NormalizedText, r.Language, r.Voice,
                   r.Model, r.Format, r.PipelineVersion, r.SourceType, r.CharacterCount, r.StorageKey,
                   inserted.AttemptCount
            FROM dbo.tts_generation_queue q
            INNER JOIN next_items n ON n.Id = q.Id
            INNER JOIN dbo.tts_audio_registry r ON r.ContentHash = q.ContentHash;
            """,
            connection);

        command.Parameters.AddWithValue("@BatchSize", Math.Max(1, batchSize));
        command.Parameters.AddWithValue("@WorkerId", workerId);
        command.Parameters.AddWithValue("@LockSeconds", Math.Max(30, (int)lockDuration.TotalSeconds));

        var items = new List<TtsQueueItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new TtsQueueItem(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9),
                reader.GetInt32(10),
                reader.GetString(11),
                reader.GetInt32(12)));
        }

        if (items.Count > 0)
        {
            await MarkRegistryProcessingAsync(connection, items.Select(x => x.ContentHash), cancellationToken);
        }

        return items;
    }

    public async Task<TtsQueueItem?> TryClaimByHashAsync(
        string contentHash,
        string workerId,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await ReleaseAbandonedProcessingAsync(connection, contentHash, cancellationToken);

        await using var command = new SqlCommand(
            """
            UPDATE q
               SET q.Status = 'processing',
                   q.WorkerId = @WorkerId,
                   q.LockedUntilUtc = DATEADD(second, @LockSeconds, SYSUTCDATETIME()),
                   q.AttemptCount = q.AttemptCount + 1,
                   q.UpdatedUtc = SYSUTCDATETIME()
            OUTPUT inserted.Id, inserted.ContentHash, r.OriginalText, r.NormalizedText, r.Language, r.Voice,
                   r.Model, r.Format, r.PipelineVersion, r.SourceType, r.CharacterCount, r.StorageKey,
                   inserted.AttemptCount
            FROM dbo.tts_generation_queue q
            INNER JOIN dbo.tts_audio_registry r ON r.ContentHash = q.ContentHash
            WHERE q.ContentHash = @ContentHash
              AND r.Status <> 'completed'
              AND (
                    (q.Status = 'pending' AND (q.NextAttemptUtc IS NULL OR q.NextAttemptUtc <= SYSUTCDATETIME()))
                    OR (q.Status = 'failed' AND q.NextAttemptUtc IS NOT NULL AND q.NextAttemptUtc <= SYSUTCDATETIME())
                    OR (q.Status = 'processing' AND q.LockedUntilUtc IS NOT NULL AND q.LockedUntilUtc <= SYSUTCDATETIME())
                  )
              AND (q.LockedUntilUtc IS NULL OR q.LockedUntilUtc <= SYSUTCDATETIME());
            """,
            connection);

        command.Parameters.AddWithValue("@ContentHash", contentHash);
        command.Parameters.AddWithValue("@WorkerId", workerId);
        command.Parameters.AddWithValue("@LockSeconds", Math.Max(30, (int)lockDuration.TotalSeconds));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var item = new TtsQueueItem(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.GetString(8),
            reader.GetString(9),
            reader.GetInt32(10),
            reader.GetString(11),
            reader.GetInt32(12));

        await reader.CloseAsync();
        await MarkRegistryProcessingAsync(connection, [contentHash], cancellationToken);
        return item;
    }

    public async Task MarkCompletedAsync(string contentHash, string storageKey, string cdnUrl, string? openAiRequestId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ExecuteNonQueryAsync(
            connection,
            (SqlTransaction)transaction,
            """
            UPDATE dbo.tts_audio_registry
               SET Status = 'completed',
                   StorageKey = @StorageKey,
                   CdnUrl = @CdnUrl,
                   OpenAiRequestId = @OpenAiRequestId,
                   ErrorMessage = NULL,
                   CompletedUtc = SYSUTCDATETIME(),
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE ContentHash = @ContentHash;

            UPDATE dbo.tts_generation_queue
               SET Status = 'completed',
                   LockedUntilUtc = NULL,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE ContentHash = @ContentHash;
            """,
            command =>
            {
                command.Parameters.AddWithValue("@ContentHash", contentHash);
                command.Parameters.AddWithValue("@StorageKey", storageKey);
                command.Parameters.AddWithValue("@CdnUrl", cdnUrl);
                command.Parameters.AddWithValue("@OpenAiRequestId", (object?)openAiRequestId ?? DBNull.Value);
            },
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(string contentHash, string errorMessage, DateTimeOffset? nextAttemptUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ExecuteNonQueryAsync(
            connection,
            (SqlTransaction)transaction,
            """
            UPDATE dbo.tts_audio_registry
               SET Status = 'failed',
                   ErrorMessage = @ErrorMessage,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE ContentHash = @ContentHash;

            UPDATE dbo.tts_generation_queue
               SET Status = 'failed',
                   ErrorMessage = @ErrorMessage,
                   NextAttemptUtc = @NextAttemptUtc,
                   LockedUntilUtc = NULL,
                   WorkerId = NULL,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE ContentHash = @ContentHash;
            """,
            command =>
            {
                command.Parameters.AddWithValue("@ContentHash", contentHash);
                command.Parameters.AddWithValue("@ErrorMessage", errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage);
                command.Parameters.AddWithValue("@NextAttemptUtc", (object?)nextAttemptUtc?.UtcDateTime ?? DBNull.Value);
            },
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<TtsUsageSummary> GetUsageSummaryAsync(DateTimeOffset sinceUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT
                SUM(CASE WHEN Status = 'pending' THEN 1 ELSE 0 END) AS PendingCount,
                SUM(CASE WHEN Status = 'processing' THEN 1 ELSE 0 END) AS ProcessingCount,
                SUM(CASE WHEN Status = 'completed' THEN 1 ELSE 0 END) AS CompletedCount,
                SUM(CASE WHEN Status = 'failed' THEN 1 ELSE 0 END) AS FailedCount,
                SUM(CASE WHEN Status = 'completed' AND CompletedUtc >= @SinceUtc THEN CharacterCount ELSE 0 END) AS CompletedCharacters,
                SUM(CASE WHEN CreatedUtc >= @SinceUtc THEN CharacterCount ELSE 0 END) AS QueuedCharacters
            FROM dbo.tts_audio_registry;
            """,
            connection);

        command.Parameters.AddWithValue("@SinceUtc", sinceUtc.UtcDateTime);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new TtsUsageSummary(0, 0, 0, 0, 0, 0);
        }

        return new TtsUsageSummary(
            reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
            reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
            reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
            reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            reader.IsDBNull(4) ? 0 : Convert.ToInt64(reader.GetValue(4)),
            reader.IsDBNull(5) ? 0 : Convert.ToInt64(reader.GetValue(5)));
    }

    public async Task<int> ReleaseStaleProcessingAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            UPDATE dbo.tts_generation_queue
               SET Status = 'pending',
                   LockedUntilUtc = NULL,
                   WorkerId = NULL,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE Status = 'processing'
               AND (
                    LockedUntilUtc IS NULL
                    OR LockedUntilUtc <= SYSUTCDATETIME()
                    OR UpdatedUtc <= DATEADD(second, -90, SYSUTCDATETIME())
               );

            UPDATE r
               SET r.Status = 'pending',
                   r.ErrorMessage = NULL,
                   r.UpdatedUtc = SYSUTCDATETIME()
              FROM dbo.tts_audio_registry r
             INNER JOIN dbo.tts_generation_queue q ON q.ContentHash = r.ContentHash
             WHERE q.Status = 'pending'
               AND r.Status = 'processing';
            """,
            connection);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<TtsBulkResetResult> ResetAllStuckAsync(bool includeFailed = false, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var queueReleased = await ExecuteNonQueryCountAsync(
            connection,
            (SqlTransaction)transaction,
            """
            UPDATE dbo.tts_generation_queue
               SET Status = 'pending',
                   LockedUntilUtc = NULL,
                   WorkerId = NULL,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE Status = 'processing';
            """,
            cancellationToken);

        var registryReset = await ExecuteNonQueryCountAsync(
            connection,
            (SqlTransaction)transaction,
            """
            UPDATE dbo.tts_audio_registry
               SET Status = 'pending',
                   ErrorMessage = NULL,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE Status = 'processing';
            """,
            cancellationToken);

        if (includeFailed)
        {
            await ExecuteNonQueryCountAsync(
                connection,
                (SqlTransaction)transaction,
                """
                UPDATE dbo.tts_generation_queue
                   SET Status = 'pending',
                       NextAttemptUtc = NULL,
                       LockedUntilUtc = NULL,
                       WorkerId = NULL,
                       ErrorMessage = NULL,
                       AttemptCount = 0,
                       UpdatedUtc = SYSUTCDATETIME()
                 WHERE Status = 'failed';

                UPDATE dbo.tts_audio_registry
                   SET Status = 'pending',
                       ErrorMessage = NULL,
                       UpdatedUtc = SYSUTCDATETIME()
                 WHERE Status = 'failed';
                """,
                cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new TtsBulkResetResult(queueReleased, registryReset);
    }

    public async Task ReleaseProcessingAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            UPDATE dbo.tts_generation_queue
               SET Status = 'pending',
                   AttemptCount = CASE WHEN AttemptCount > 0 THEN AttemptCount - 1 ELSE 0 END,
                   LockedUntilUtc = NULL,
                   WorkerId = NULL,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE ContentHash = @ContentHash
               AND Status = 'processing';

            UPDATE dbo.tts_audio_registry
               SET Status = 'pending',
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE ContentHash = @ContentHash
               AND Status = 'processing';
            """,
            connection);
        command.Parameters.AddWithValue("@ContentHash", contentHash);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ReleaseAbandonedProcessingAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await ReleaseAbandonedProcessingAsync(connection, contentHash, cancellationToken);
    }

    public async Task<int> ReplayFailedAsync(int maxItems, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            UPDATE TOP (@MaxItems) q
               SET q.Status = 'pending',
                   q.NextAttemptUtc = NULL,
                   q.LockedUntilUtc = NULL,
                   q.WorkerId = NULL,
                   q.ErrorMessage = NULL,
                   q.UpdatedUtc = SYSUTCDATETIME()
              FROM dbo.tts_generation_queue q
             WHERE q.Status = 'failed';

            UPDATE r
               SET r.Status = 'pending',
                   r.ErrorMessage = NULL,
                   r.UpdatedUtc = SYSUTCDATETIME()
              FROM dbo.tts_audio_registry r
             INNER JOIN dbo.tts_generation_queue q ON q.ContentHash = r.ContentHash
             WHERE q.Status = 'pending'
               AND r.Status = 'failed';
            """,
            connection);
        command.Parameters.AddWithValue("@MaxItems", Math.Clamp(maxItems, 1, 500));
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<TtsAudioRecord?> GetByHashAsync(SqlConnection connection, SqlTransaction transaction, string contentHash, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            """
            SELECT TOP (1)
                Id, ContentHash, OriginalText, NormalizedText, Language, Voice, Model, Format,
                PipelineVersion, SourceType, Status, CharacterCount, StorageKey, CdnUrl,
                OpenAiRequestId, ErrorMessage, CreatedUtc, UpdatedUtc, CompletedUtc
            FROM dbo.tts_audio_registry WITH (UPDLOCK, HOLDLOCK)
            WHERE ContentHash = @ContentHash;
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("@ContentHash", contentHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapRecord(reader) : null;
    }

    private static async Task InsertRegistryAsync(SqlConnection connection, SqlTransaction transaction, TtsAudioDescriptor descriptor, CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(
            connection,
            transaction,
            """
            INSERT INTO dbo.tts_audio_registry
            (
                ContentHash, OriginalText, NormalizedText, Language, Voice, Model, Format,
                PipelineVersion, SourceType, Status, CharacterCount, StorageKey,
                CreatedUtc, UpdatedUtc
            )
            VALUES
            (
                @ContentHash, @OriginalText, @NormalizedText, @Language, @Voice, @Model, @Format,
                @PipelineVersion, @SourceType, 'pending', @CharacterCount, @StorageKey,
                SYSUTCDATETIME(), SYSUTCDATETIME()
            );
            """,
            command => AddDescriptorParameters(command, descriptor),
            cancellationToken);
    }

    private static async Task EnqueueIfNeededAsync(SqlConnection connection, SqlTransaction transaction, TtsAudioDescriptor descriptor, CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(
            connection,
            transaction,
            """
            IF EXISTS
            (
                SELECT 1
                FROM dbo.tts_generation_queue WITH (UPDLOCK, HOLDLOCK)
                WHERE ContentHash = @ContentHash AND Status = 'failed'
            )
            BEGIN
                UPDATE dbo.tts_generation_queue
                   SET Status = 'pending',
                       NextAttemptUtc = NULL,
                       LockedUntilUtc = NULL,
                       WorkerId = NULL,
                       AttemptCount = 0,
                       ErrorMessage = NULL,
                       UpdatedUtc = SYSUTCDATETIME()
                 WHERE ContentHash = @ContentHash AND Status = 'failed';
            END
            ELSE IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.tts_generation_queue WITH (UPDLOCK, HOLDLOCK)
                WHERE ContentHash = @ContentHash AND Status IN ('pending', 'processing')
            )
            BEGIN
                INSERT INTO dbo.tts_generation_queue
                    (ContentHash, Status, Priority, AttemptCount, CreatedUtc, UpdatedUtc)
                VALUES
                    (@ContentHash, 'pending', 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
            END
            """,
            command => command.Parameters.AddWithValue("@ContentHash", descriptor.ContentHash),
            cancellationToken);
    }

    private static async Task ForceReleaseProcessingForUserRequestAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string contentHash,
        CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(
            connection,
            transaction,
            """
            UPDATE dbo.tts_generation_queue
               SET Status = 'pending',
                   LockedUntilUtc = NULL,
                   WorkerId = NULL,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE ContentHash = @ContentHash
               AND Status = 'processing';

            UPDATE dbo.tts_audio_registry
               SET Status = 'pending',
                   ErrorMessage = NULL,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE ContentHash = @ContentHash
               AND Status = 'processing';
            """,
            command => command.Parameters.AddWithValue("@ContentHash", contentHash),
            cancellationToken);
    }

    private static async Task ReleaseStaleProcessingAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string contentHash,
        CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(
            connection,
            transaction,
            """
            UPDATE dbo.tts_generation_queue
               SET Status = 'pending',
                   LockedUntilUtc = NULL,
                   WorkerId = NULL,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE ContentHash = @ContentHash
               AND Status = 'processing'
               AND (
                    LockedUntilUtc IS NULL
                    OR LockedUntilUtc <= SYSUTCDATETIME()
                    OR UpdatedUtc <= DATEADD(second, -90, SYSUTCDATETIME())
               );
            """,
            command => command.Parameters.AddWithValue("@ContentHash", contentHash),
            cancellationToken);
    }

    private static async Task ReleaseAbandonedProcessingAsync(
        SqlConnection connection,
        string contentHash,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            """
            UPDATE dbo.tts_generation_queue
               SET Status = 'pending',
                   LockedUntilUtc = NULL,
                   WorkerId = NULL,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE ContentHash = @ContentHash
               AND Status = 'processing'
               AND (
                    LockedUntilUtc IS NULL
                    OR LockedUntilUtc <= SYSUTCDATETIME()
                    OR UpdatedUtc <= DATEADD(second, -90, SYSUTCDATETIME())
               );

            UPDATE dbo.tts_audio_registry
               SET Status = 'pending',
                   ErrorMessage = NULL,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE ContentHash = @ContentHash
               AND Status = 'processing'
               AND UpdatedUtc <= DATEADD(second, -90, SYSUTCDATETIME());
            """,
            connection);
        command.Parameters.AddWithValue("@ContentHash", contentHash);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkRegistryProcessingAsync(
        SqlConnection connection,
        IEnumerable<string> contentHashes,
        CancellationToken cancellationToken)
    {
        var hashes = contentHashes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (hashes.Length == 0)
        {
            return;
        }

        var parameterNames = hashes.Select((_, index) => $"@Hash{index}").ToArray();
        await using var command = new SqlCommand(
            $"""
            UPDATE dbo.tts_audio_registry
               SET Status = 'processing',
                   ErrorMessage = NULL,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE ContentHash IN ({string.Join(", ", parameterNames)})
               AND Status IN ('pending', 'failed');
            """,
            connection);

        for (var i = 0; i < hashes.Length; i++)
        {
            command.Parameters.AddWithValue(parameterNames[i], hashes[i]);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ResetFailedRegistryAsync(SqlConnection connection, SqlTransaction transaction, string contentHash, CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(
            connection,
            transaction,
            """
            UPDATE dbo.tts_audio_registry
               SET Status = 'pending',
                   ErrorMessage = NULL,
                   UpdatedUtc = SYSUTCDATETIME()
             WHERE ContentHash = @ContentHash AND Status = 'failed';
            """,
            command => command.Parameters.AddWithValue("@ContentHash", contentHash),
            cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryCountAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection, transaction);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteNonQueryAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql,
        Action<SqlCommand> configure,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection, transaction);
        configure(command);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddDescriptorParameters(SqlCommand command, TtsAudioDescriptor descriptor)
    {
        command.Parameters.AddWithValue("@ContentHash", descriptor.ContentHash);
        command.Parameters.AddWithValue("@OriginalText", descriptor.OriginalText);
        command.Parameters.AddWithValue("@NormalizedText", descriptor.NormalizedText);
        command.Parameters.AddWithValue("@Language", descriptor.Language);
        command.Parameters.AddWithValue("@Voice", descriptor.Voice);
        command.Parameters.AddWithValue("@Model", descriptor.Model);
        command.Parameters.AddWithValue("@Format", descriptor.Format);
        command.Parameters.AddWithValue("@PipelineVersion", descriptor.PipelineVersion);
        command.Parameters.AddWithValue("@SourceType", descriptor.SourceType);
        command.Parameters.AddWithValue("@CharacterCount", descriptor.CharacterCount);
        command.Parameters.AddWithValue("@StorageKey", descriptor.StorageKey);
    }

    private static TtsAudioRecord MapRecord(SqlDataReader reader)
    {
        return new TtsAudioRecord(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.GetString(8),
            reader.GetString(9),
            reader.GetString(10),
            reader.GetInt32(11),
            reader.IsDBNull(12) ? null : reader.GetString(12),
            reader.IsDBNull(13) ? null : reader.GetString(13),
            reader.IsDBNull(14) ? null : reader.GetString(14),
            reader.IsDBNull(15) ? null : reader.GetString(15),
            new DateTimeOffset(reader.GetDateTime(16), TimeSpan.Zero),
            new DateTimeOffset(reader.GetDateTime(17), TimeSpan.Zero),
            reader.IsDBNull(18) ? null : new DateTimeOffset(reader.GetDateTime(18), TimeSpan.Zero));
    }
}
