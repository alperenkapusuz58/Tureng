IF OBJECT_ID(N'dbo.tts_audio_registry', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tts_audio_registry
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tts_audio_registry PRIMARY KEY,
        ContentHash CHAR(64) NOT NULL,
        OriginalText NVARCHAR(1000) NOT NULL,
        NormalizedText NVARCHAR(1000) NOT NULL,
        Language NVARCHAR(16) NOT NULL,
        Voice NVARCHAR(64) NOT NULL,
        Model NVARCHAR(64) NOT NULL,
        Format NVARCHAR(16) NOT NULL,
        PipelineVersion NVARCHAR(32) NOT NULL,
        SourceType NVARCHAR(32) NOT NULL,
        Status NVARCHAR(32) NOT NULL,
        CharacterCount INT NOT NULL,
        StorageKey NVARCHAR(512) NULL,
        CdnUrl NVARCHAR(1024) NULL,
        OpenAiRequestId NVARCHAR(128) NULL,
        ErrorMessage NVARCHAR(2000) NULL,
        CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_tts_audio_registry_CreatedUtc DEFAULT SYSUTCDATETIME(),
        UpdatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_tts_audio_registry_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        CompletedUtc DATETIME2(3) NULL,
        CONSTRAINT CK_tts_audio_registry_Status CHECK (Status IN ('pending', 'processing', 'completed', 'failed')),
        CONSTRAINT CK_tts_audio_registry_CharacterCount CHECK (CharacterCount >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tts_audio_registry_ContentHash' AND object_id = OBJECT_ID(N'dbo.tts_audio_registry'))
BEGIN
    CREATE UNIQUE INDEX UX_tts_audio_registry_ContentHash ON dbo.tts_audio_registry(ContentHash);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tts_audio_registry_Status_UpdatedUtc' AND object_id = OBJECT_ID(N'dbo.tts_audio_registry'))
BEGIN
    CREATE INDEX IX_tts_audio_registry_Status_UpdatedUtc ON dbo.tts_audio_registry(Status, UpdatedUtc);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tts_audio_registry_CompletedUtc' AND object_id = OBJECT_ID(N'dbo.tts_audio_registry'))
BEGIN
    CREATE INDEX IX_tts_audio_registry_CompletedUtc ON dbo.tts_audio_registry(CompletedUtc) INCLUDE (CharacterCount, Status);
END;
GO

IF OBJECT_ID(N'dbo.tts_generation_queue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tts_generation_queue
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tts_generation_queue PRIMARY KEY,
        ContentHash CHAR(64) NOT NULL,
        Status NVARCHAR(32) NOT NULL,
        Priority INT NOT NULL CONSTRAINT DF_tts_generation_queue_Priority DEFAULT 0,
        AttemptCount INT NOT NULL CONSTRAINT DF_tts_generation_queue_AttemptCount DEFAULT 0,
        NextAttemptUtc DATETIME2(3) NULL,
        LockedUntilUtc DATETIME2(3) NULL,
        WorkerId NVARCHAR(128) NULL,
        ErrorMessage NVARCHAR(2000) NULL,
        CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_tts_generation_queue_CreatedUtc DEFAULT SYSUTCDATETIME(),
        UpdatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_tts_generation_queue_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_tts_generation_queue_Status CHECK (Status IN ('pending', 'processing', 'completed', 'failed')),
        CONSTRAINT CK_tts_generation_queue_AttemptCount CHECK (AttemptCount >= 0),
        CONSTRAINT FK_tts_generation_queue_registry FOREIGN KEY (ContentHash)
            REFERENCES dbo.tts_audio_registry(ContentHash)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tts_generation_queue_ActiveContentHash' AND object_id = OBJECT_ID(N'dbo.tts_generation_queue'))
BEGIN
    CREATE UNIQUE INDEX UX_tts_generation_queue_ActiveContentHash
        ON dbo.tts_generation_queue(ContentHash)
        WHERE Status <> 'completed';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tts_generation_queue_Polling' AND object_id = OBJECT_ID(N'dbo.tts_generation_queue'))
BEGIN
    CREATE INDEX IX_tts_generation_queue_Polling
        ON dbo.tts_generation_queue(Status, NextAttemptUtc, LockedUntilUtc, Priority, CreatedUtc)
        INCLUDE (ContentHash, AttemptCount);
END;
GO
