-- =======================================================
-- Table: identity.EmailVerificationToken
-- Description: Tokens for email verification
-- =======================================================
CREATE TABLE [identity].[EmailVerificationToken]
(
    [TokenId]               BIGINT IDENTITY(1,1)    NOT NULL,
    [UserId]                BIGINT                  NOT NULL,
    [TokenHash]             NVARCHAR(500)           NOT NULL,
    [ExpiresAtUtc]          DATETIME2(7)            NOT NULL,
    [IsUsed]                BIT                     NOT NULL    CONSTRAINT [DF_EmailVerificationToken_IsUsed] DEFAULT (0),
    [UsedAtUtc]             DATETIME2(7)            NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_EmailVerificationToken_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_EmailVerificationToken] PRIMARY KEY CLUSTERED ([TokenId]),
    CONSTRAINT [FK_EmailVerificationToken_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([UserId])
);
GO

-- Index for token lookups by user
CREATE NONCLUSTERED INDEX [IX_EmailVerificationToken_UserId]
    ON [identity].[EmailVerificationToken]([UserId])
    WHERE [IsUsed] = 0;
GO

-- Index for cleanup of expired tokens
CREATE NONCLUSTERED INDEX [IX_EmailVerificationToken_ExpiresAtUtc]
    ON [identity].[EmailVerificationToken]([ExpiresAtUtc])
    WHERE [IsUsed] = 0;
GO
