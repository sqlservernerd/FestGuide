-- =======================================================
-- Table: integrations.WebhookDelivery
-- Description: Log of webhook delivery attempts
-- =======================================================
CREATE TABLE [integrations].[WebhookDelivery]
(
    [WebhookDeliveryId]     BIGINT IDENTITY(1,1)    NOT NULL,
    [WebhookSubscriptionId] BIGINT                  NULL,
    [EventType]             NVARCHAR(100)           NOT NULL,   -- 'schedule.published', 'artist.updated', etc.
    [Payload]               NVARCHAR(MAX)           NOT NULL,   -- JSON payload sent
    [ResponseStatusCode]    INT                     NULL,
    [ResponseBody]          NVARCHAR(MAX)           NULL,
    [IsSuccess]             BIT                     NOT NULL    CONSTRAINT [DF_WebhookDelivery_IsSuccess] DEFAULT (0),
    [ErrorMessage]          NVARCHAR(500)           NULL,
    [AttemptNumber]         INT                     NOT NULL    CONSTRAINT [DF_WebhookDelivery_AttemptNumber] DEFAULT (1),
    [DurationMs]            INT                     NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_WebhookDelivery_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_WebhookDelivery] PRIMARY KEY CLUSTERED ([WebhookDeliveryId]),
    CONSTRAINT [FK_WebhookDelivery_WebhookSubscription] FOREIGN KEY ([WebhookSubscriptionId]) REFERENCES [integrations].[WebhookSubscription]([WebhookSubscriptionId]) ON DELETE SET NULL
);
GO

-- Index for subscription delivery history
CREATE NONCLUSTERED INDEX [IX_WebhookDelivery_WebhookSubscriptionId]
    ON [integrations].[WebhookDelivery]([WebhookSubscriptionId], [CreatedAtUtc] DESC);
GO

-- Index for failed deliveries (for retry processing)
CREATE NONCLUSTERED INDEX [IX_WebhookDelivery_IsSuccess]
    ON [integrations].[WebhookDelivery]([IsSuccess], [CreatedAtUtc])
    WHERE [IsSuccess] = 0;
GO

-- Index for cleanup of old deliveries
CREATE NONCLUSTERED INDEX [IX_WebhookDelivery_CreatedAtUtc]
    ON [integrations].[WebhookDelivery]([CreatedAtUtc]);
GO
