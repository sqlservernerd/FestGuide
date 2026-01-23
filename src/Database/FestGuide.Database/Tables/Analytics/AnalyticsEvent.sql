-- =======================================================
-- Table: analytics.AnalyticsEvent
-- Description: Analytics events for tracking user interactions
-- =======================================================
CREATE TABLE [analytics].[AnalyticsEvent]
(
    [AnalyticsEventId]      BIGINT IDENTITY(1,1)    NOT NULL,
    [UserId]                BIGINT                  NULL,       -- NULL for anonymous users
    [FestivalId]            BIGINT                  NULL,
    [EditionId]             BIGINT                  NULL,
    [EventType]             NVARCHAR(100)           NOT NULL,   -- 'schedule_view', 'artist_save', 'engagement_add', etc.
    [EntityType]            NVARCHAR(50)            NULL,       -- 'Schedule', 'Artist', 'Engagement', etc.
    [EntityId]              BIGINT                  NULL,
    [Metadata]              NVARCHAR(MAX)           NULL,       -- JSON metadata
    [Platform]              NVARCHAR(20)            NULL,       -- 'ios', 'android', 'web'
    [DeviceType]            NVARCHAR(50)            NULL,
    [SessionId]             NVARCHAR(100)           NULL,
    [EventTimestampUtc]     DATETIME2(7)            NOT NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_AnalyticsEvent_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_AnalyticsEvent] PRIMARY KEY CLUSTERED ([AnalyticsEventId]),
    CONSTRAINT [FK_AnalyticsEvent_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_AnalyticsEvent_Festival] FOREIGN KEY ([FestivalId]) REFERENCES [core].[Festival]([FestivalId]),
    CONSTRAINT [FK_AnalyticsEvent_Edition] FOREIGN KEY ([EditionId]) REFERENCES [core].[FestivalEdition]([EditionId])
);
GO

-- Index for user event lookups
CREATE NONCLUSTERED INDEX [IX_AnalyticsEvent_UserId]
    ON [analytics].[AnalyticsEvent]([UserId], [EventTimestampUtc] DESC)
    WHERE [UserId] IS NOT NULL;
GO

-- Index for festival event lookups
CREATE NONCLUSTERED INDEX [IX_AnalyticsEvent_FestivalId]
    ON [analytics].[AnalyticsEvent]([FestivalId], [EventTimestampUtc] DESC)
    WHERE [FestivalId] IS NOT NULL;
GO

-- Index for edition event lookups
CREATE NONCLUSTERED INDEX [IX_AnalyticsEvent_EditionId]
    ON [analytics].[AnalyticsEvent]([EditionId], [EventTimestampUtc] DESC)
    WHERE [EditionId] IS NOT NULL;
GO

-- Index for event type analytics
CREATE NONCLUSTERED INDEX [IX_AnalyticsEvent_EventType]
    ON [analytics].[AnalyticsEvent]([EventType], [EventTimestampUtc] DESC);
GO

-- Index for time-based queries (reporting)
CREATE NONCLUSTERED INDEX [IX_AnalyticsEvent_EventTimestampUtc]
    ON [analytics].[AnalyticsEvent]([EventTimestampUtc] DESC)
    INCLUDE ([EventType], [FestivalId], [EditionId]);
GO

-- Index for session analysis
CREATE NONCLUSTERED INDEX [IX_AnalyticsEvent_SessionId]
    ON [analytics].[AnalyticsEvent]([SessionId], [EventTimestampUtc])
    WHERE [SessionId] IS NOT NULL;
GO
