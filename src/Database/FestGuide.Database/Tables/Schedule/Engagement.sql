-- =======================================================
-- Table: schedule.Engagement
-- Description: Artists assigned to time slots
-- =======================================================
CREATE TABLE [schedule].[Engagement]
(
    [EngagementId]          UNIQUEIDENTIFIER    NOT NULL,
    [TimeSlotId]            UNIQUEIDENTIFIER    NOT NULL,
    [ArtistId]              UNIQUEIDENTIFIER    NOT NULL,
    [Notes]                 NVARCHAR(MAX)       NULL,
    [IsDeleted]             BIT                 NOT NULL    CONSTRAINT [DF_Engagement_IsDeleted] DEFAULT (0),
    [DeletedAtUtc]          DATETIME2(7)        NULL,
    [CreatedAtUtc]          DATETIME2(7)        NOT NULL    CONSTRAINT [DF_Engagement_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             UNIQUEIDENTIFIER    NULL,
    [ModifiedAtUtc]         DATETIME2(7)        NOT NULL    CONSTRAINT [DF_Engagement_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            UNIQUEIDENTIFIER    NULL,

    CONSTRAINT [PK_Engagement] PRIMARY KEY CLUSTERED ([EngagementId]),
    CONSTRAINT [FK_Engagement_TimeSlot] FOREIGN KEY ([TimeSlotId]) REFERENCES [venue].[TimeSlot]([TimeSlotId]),
    CONSTRAINT [FK_Engagement_Artist] FOREIGN KEY ([ArtistId]) REFERENCES [core].[Artist]([ArtistId])
);
GO

-- Index for time slot engagement lookups
CREATE NONCLUSTERED INDEX [IX_Engagement_TimeSlotId]
    ON [schedule].[Engagement]([TimeSlotId])
    WHERE [IsDeleted] = 0;
GO

-- Index for artist engagement lookups
CREATE NONCLUSTERED INDEX [IX_Engagement_ArtistId]
    ON [schedule].[Engagement]([ArtistId])
    WHERE [IsDeleted] = 0;
GO

-- Unique constraint: one artist per time slot
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Engagement_TimeSlotId_ArtistId]
    ON [schedule].[Engagement]([TimeSlotId], [ArtistId])
    WHERE [IsDeleted] = 0;
GO
