-- =======================================================
-- Table: integrations.WebhookSubscription
-- Description: Webhook subscriptions for event notifications
-- =======================================================
CREATE TABLE [integrations].[WebhookSubscription]
(
    [WebhookSubscriptionId] BIGINT IDENTITY(1,1)    NOT NULL,
    [FestivalId]            BIGINT                  NOT NULL,
    [Name]                  NVARCHAR(100)           NOT NULL,
    [Url]                   NVARCHAR(2083)          NOT NULL,
    [SecretHash]            NVARCHAR(500)           NOT NULL,   -- HMAC secret hash for signature verification
    [Events]                NVARCHAR(500)           NOT NULL,   -- Comma-separated: 'schedule.published,artist.updated'
    [IsActive]              BIT                     NOT NULL    CONSTRAINT [DF_WebhookSubscription_IsActive] DEFAULT (1),
    [LastTriggeredAtUtc]    DATETIME2(7)            NULL,
    [LastSuccessAtUtc]      DATETIME2(7)            NULL,
    [LastFailureAtUtc]      DATETIME2(7)            NULL,
    [LastFailureReason]     NVARCHAR(500)           NULL,
    [ConsecutiveFailures]   INT                     NOT NULL    CONSTRAINT [DF_WebhookSubscription_ConsecutiveFailures] DEFAULT (0),
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_WebhookSubscription_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             BIGINT                  NULL,
    [ModifiedAtUtc]         DATETIME2(7)            NOT NULL    CONSTRAINT [DF_WebhookSubscription_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            BIGINT                  NULL,

    CONSTRAINT [PK_WebhookSubscription] PRIMARY KEY CLUSTERED ([WebhookSubscriptionId]),
    CONSTRAINT [FK_WebhookSubscription_Festival] FOREIGN KEY ([FestivalId]) REFERENCES [core].[Festival]([FestivalId]),
    CONSTRAINT [FK_WebhookSubscription_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_WebhookSubscription_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [identity].[User]([UserId])
);
GO

-- Index for festival webhook lookups
CREATE NONCLUSTERED INDEX [IX_WebhookSubscription_FestivalId]
    ON [integrations].[WebhookSubscription]([FestivalId])
    WHERE [IsActive] = 1;
GO

-- Index for event type filtering
CREATE NONCLUSTERED INDEX [IX_WebhookSubscription_IsActive]
    ON [integrations].[WebhookSubscription]([IsActive])
    INCLUDE ([FestivalId], [Url], [Events]);
GO
