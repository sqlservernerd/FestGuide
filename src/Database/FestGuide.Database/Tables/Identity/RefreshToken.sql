-- =======================================================
-- Table: identity.RefreshToken
-- Description: Refresh tokens for JWT authentication
-- =======================================================
CREATE TABLE [identity].[RefreshToken]
(
    [RefreshTokenId]        BIGINT IDENTITY(1,1)    NOT NULL,
    [UserId]                BIGINT                  NOT NULL,
    [TokenHash]             NVARCHAR(500)           NOT NULL,
    [ExpiresAtUtc]          DATETIME2(7)            NOT NULL,
    [IsRevoked]             BIT                     NOT NULL    CONSTRAINT [DF_RefreshToken_IsRevoked] DEFAULT (0),
    [RevokedAtUtc]          DATETIME2(7)            NULL,
    [ReplacedByTokenId]     BIGINT                  NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_RefreshToken_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedByIp]           NVARCHAR(50)            NULL,

    CONSTRAINT [PK_RefreshToken] PRIMARY KEY CLUSTERED ([RefreshTokenId]),
    CONSTRAINT [FK_RefreshToken_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_RefreshToken_ReplacedByToken] FOREIGN KEY ([ReplacedByTokenId]) REFERENCES [identity].[RefreshToken]([RefreshTokenId]) ON DELETE SET NULL
);
GO

-- Index for token lookups by user
CREATE NONCLUSTERED INDEX [IX_RefreshToken_UserId]
    ON [identity].[RefreshToken]([UserId])
    INCLUDE ([TokenHash], [ExpiresAtUtc], [IsRevoked]);
GO

-- Index for active token lookups
CREATE NONCLUSTERED INDEX [IX_RefreshToken_TokenHash]
    ON [identity].[RefreshToken]([TokenHash])
    WHERE [IsRevoked] = 0;
GO

-- Index for cleanup of expired tokens
CREATE NONCLUSTERED INDEX [IX_RefreshToken_ExpiresAtUtc]
    ON [identity].[RefreshToken]([ExpiresAtUtc])
    WHERE [IsRevoked] = 0;
GO

-- Index for token replacement chain lookups
CREATE NONCLUSTERED INDEX [IX_RefreshToken_ReplacedByTokenId]
    ON [identity].[RefreshToken]([ReplacedByTokenId])
    WHERE [ReplacedByTokenId] IS NOT NULL;
GO
