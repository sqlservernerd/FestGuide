-- =======================================================
-- Table: notifications.NotificationLog
-- Description: Log of sent notifications
-- =======================================================
CREATE TABLE [notifications].[NotificationLog]
(
    [NotificationLogId]     BIGINT IDENTITY(1,1)    NOT NULL,
    [UserId]                BIGINT                  NOT NULL,
    [DeviceTokenId]         BIGINT                  NULL,
    [NotificationType]      NVARCHAR(50)            NOT NULL,   -- 'schedule_change', 'reminder', 'announcement', etc.
    [Title]                 NVARCHAR(200)           NOT NULL,
    [Body]                  NVARCHAR(MAX)           NOT NULL,
    [DataPayload]           NVARCHAR(MAX)           NULL,       -- JSON payload
    [RelatedEntityType]     NVARCHAR(50)            NULL,       -- 'Edition', 'Engagement', 'TimeSlot', etc.
    [RelatedEntityId]       BIGINT                  NULL,
    [SentAtUtc]             DATETIME2(7)            NOT NULL    CONSTRAINT [DF_NotificationLog_SentAtUtc] DEFAULT (SYSUTCDATETIME()),
    [IsDelivered]           BIT                     NOT NULL    CONSTRAINT [DF_NotificationLog_IsDelivered] DEFAULT (0),
    [ErrorMessage]          NVARCHAR(MAX)           NULL,
    [ReadAtUtc]             DATETIME2(7)            NULL,
    [IsDeleted]             BIT                     NOT NULL    CONSTRAINT [DF_NotificationLog_IsDeleted] DEFAULT (0),
    [DeletedAtUtc]          DATETIME2(7)            NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_NotificationLog_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             BIGINT                  NULL,
    [ModifiedAtUtc]         DATETIME2(7)            NOT NULL    CONSTRAINT [DF_NotificationLog_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            BIGINT                  NULL,

    CONSTRAINT [PK_NotificationLog] PRIMARY KEY CLUSTERED ([NotificationLogId]),
    CONSTRAINT [FK_NotificationLog_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_NotificationLog_DeviceToken] FOREIGN KEY ([DeviceTokenId]) REFERENCES [notifications].[DeviceToken]([DeviceTokenId]),
    CONSTRAINT [FK_NotificationLog_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_NotificationLog_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [identity].[User]([UserId])
);
GO

-- Index for user notification lookups
CREATE NONCLUSTERED INDEX [IX_NotificationLog_UserId]
    ON [notifications].[NotificationLog]([UserId], [SentAtUtc] DESC);
GO

-- Index for unread notifications
CREATE NONCLUSTERED INDEX [IX_NotificationLog_UserId_ReadAtUtc]
    ON [notifications].[NotificationLog]([UserId])
    WHERE [ReadAtUtc] IS NULL;
GO

-- Index for notification type queries
CREATE NONCLUSTERED INDEX [IX_NotificationLog_NotificationType]
    ON [notifications].[NotificationLog]([NotificationType], [SentAtUtc] DESC);
GO

-- Index for delivery status monitoring
CREATE NONCLUSTERED INDEX [IX_NotificationLog_IsDelivered]
    ON [notifications].[NotificationLog]([IsDelivered], [SentAtUtc])
    WHERE [IsDelivered] = 0;
GO
