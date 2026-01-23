-- =======================================================
-- Table: notifications.DeviceToken
-- Description: Devices registered for push notifications
-- =======================================================
CREATE TABLE [notifications].[DeviceToken]
(
    [DeviceTokenId]         BIGINT IDENTITY(1,1)    NOT NULL,
    [UserId]                BIGINT                  NOT NULL,
    [Token]                 NVARCHAR(500)           NOT NULL,
    [Platform]              NVARCHAR(20)            NOT NULL,   -- 'ios', 'android', 'web'
    [DeviceName]            NVARCHAR(100)           NULL,
    [IsActive]              BIT                     NOT NULL    CONSTRAINT [DF_DeviceToken_IsActive] DEFAULT (1),
    [LastUsedAtUtc]         DATETIME2(7)            NULL,
    [ExpiresAtUtc]          DATETIME2(7)            NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_DeviceToken_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             BIGINT                  NULL,
    [ModifiedAtUtc]         DATETIME2(7)            NOT NULL    CONSTRAINT [DF_DeviceToken_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            BIGINT                  NULL,

    CONSTRAINT [PK_DeviceToken] PRIMARY KEY CLUSTERED ([DeviceTokenId]),
    CONSTRAINT [FK_DeviceToken_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_DeviceToken_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_DeviceToken_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [CK_DeviceToken_Platform] CHECK ([Platform] IN ('ios', 'android', 'web'))
);
GO

-- Index for user device lookups
CREATE NONCLUSTERED INDEX [IX_DeviceToken_UserId]
    ON [notifications].[DeviceToken]([UserId])
    WHERE [IsActive] = 1;
GO

-- Unique constraint: one token per device
CREATE UNIQUE NONCLUSTERED INDEX [UQ_DeviceToken_Token]
    ON [notifications].[DeviceToken]([Token]);
GO

-- Index for cleanup of expired tokens
CREATE NONCLUSTERED INDEX [IX_DeviceToken_ExpiresAtUtc]
    ON [notifications].[DeviceToken]([ExpiresAtUtc])
    WHERE [IsActive] = 1 AND [ExpiresAtUtc] IS NOT NULL;
GO
