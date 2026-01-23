-- =======================================================
-- Table: identity.PasswordResetToken
-- Description: Tokens for password reset
-- =======================================================
CREATE TABLE [identity].[PasswordResetToken]
(
    [TokenId]               BIGINT IDENTITY(1,1)    NOT NULL,
    [UserId]                BIGINT                  NOT NULL,
    [TokenHash]             NVARCHAR(500)           NOT NULL,
    [ExpiresAtUtc]          DATETIME2(7)            NOT NULL,
    [IsUsed]                BIT                     NOT NULL    CONSTRAINT [DF_PasswordResetToken_IsUsed] DEFAULT (0),
    [UsedAtUtc]             DATETIME2(7)            NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_PasswordResetToken_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_PasswordResetToken] PRIMARY KEY CLUSTERED ([TokenId]),
    CONSTRAINT [FK_PasswordResetToken_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([UserId])
);
GO

-- Index for token lookups by user
CREATE NONCLUSTERED INDEX [IX_PasswordResetToken_UserId]
    ON [identity].[PasswordResetToken]([UserId])
    WHERE [IsUsed] = 0;
GO

-- Index for cleanup of expired tokens
CREATE NONCLUSTERED INDEX [IX_PasswordResetToken_ExpiresAtUtc]
    ON [identity].[PasswordResetToken]([ExpiresAtUtc])
    WHERE [IsUsed] = 0;
GO
