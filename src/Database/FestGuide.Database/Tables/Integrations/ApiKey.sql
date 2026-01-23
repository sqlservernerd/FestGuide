-- =======================================================
-- Table: integrations.ApiKey
-- Description: API keys for external integrations
-- =======================================================
CREATE TABLE [integrations].[ApiKey]
(
    [ApiKeyId]              UNIQUEIDENTIFIER    NOT NULL,
    [FestivalId]            UNIQUEIDENTIFIER    NOT NULL,
    [Name]                  NVARCHAR(100)       NOT NULL,
    [KeyHash]               NVARCHAR(500)       NOT NULL,   -- SHA-256 hash of the key
    [KeyPrefix]             NVARCHAR(10)        NOT NULL,   -- First 8 chars for identification
    [Scopes]                NVARCHAR(500)       NULL,       -- Comma-separated scopes: 'read:schedule,read:artists'
    [ExpiresAtUtc]          DATETIME2(7)        NULL,
    [IsRevoked]             BIT                 NOT NULL    CONSTRAINT [DF_ApiKey_IsRevoked] DEFAULT (0),
    [RevokedAtUtc]          DATETIME2(7)        NULL,
    [RevokedBy]             UNIQUEIDENTIFIER    NULL,
    [LastUsedAtUtc]         DATETIME2(7)        NULL,
    [UsageCount]            BIGINT              NOT NULL    CONSTRAINT [DF_ApiKey_UsageCount] DEFAULT (0),
    [RateLimitPerMinute]    INT                 NULL,       -- NULL = use default
    [CreatedAtUtc]          DATETIME2(7)        NOT NULL    CONSTRAINT [DF_ApiKey_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             UNIQUEIDENTIFIER    NOT NULL,
    [ModifiedAtUtc]         DATETIME2(7)        NOT NULL    CONSTRAINT [DF_ApiKey_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            UNIQUEIDENTIFIER    NULL,

    CONSTRAINT [PK_ApiKey] PRIMARY KEY CLUSTERED ([ApiKeyId]),
    CONSTRAINT [FK_ApiKey_Festival] FOREIGN KEY ([FestivalId]) REFERENCES [core].[Festival]([FestivalId]),
    CONSTRAINT [FK_ApiKey_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_ApiKey_RevokedBy] FOREIGN KEY ([RevokedBy]) REFERENCES [identity].[User]([UserId])
);
GO

-- Index for festival API key lookups
CREATE NONCLUSTERED INDEX [IX_ApiKey_FestivalId]
    ON [integrations].[ApiKey]([FestivalId])
    WHERE [IsRevoked] = 0;
GO

-- Index for key prefix lookups (used during authentication)
CREATE NONCLUSTERED INDEX [IX_ApiKey_KeyPrefix]
    ON [integrations].[ApiKey]([KeyPrefix])
    WHERE [IsRevoked] = 0;
GO

-- Index for expiration cleanup
CREATE NONCLUSTERED INDEX [IX_ApiKey_ExpiresAtUtc]
    ON [integrations].[ApiKey]([ExpiresAtUtc])
    WHERE [IsRevoked] = 0 AND [ExpiresAtUtc] IS NOT NULL;
GO

-- Index for API keys created by user
CREATE NONCLUSTERED INDEX [IX_ApiKey_CreatedBy]
    ON [integrations].[ApiKey]([CreatedBy]);
GO

-- Index for API keys revoked by user
CREATE NONCLUSTERED INDEX [IX_ApiKey_RevokedBy]
    ON [integrations].[ApiKey]([RevokedBy])
    WHERE [RevokedBy] IS NOT NULL;
GO
