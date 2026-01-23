-- =======================================================
-- Table: schedule.Engagement
-- Description: Artists assigned to time slots
-- =======================================================
CREATE TABLE [schedule].[Engagement]
(
    [EngagementId]          BIGINT IDENTITY(1,1)    NOT NULL,
    [TimeSlotId]            BIGINT                  NOT NULL,
    [ArtistId]              BIGINT                  NOT NULL,
    [Notes]                 NVARCHAR(MAX)           NULL,
    [IsDeleted]             BIT                     NOT NULL    CONSTRAINT [DF_Engagement_IsDeleted] DEFAULT (0),
    [DeletedAtUtc]          DATETIME2(7)            NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_Engagement_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             BIGINT                  NULL,
    [ModifiedAtUtc]         DATETIME2(7)            NOT NULL    CONSTRAINT [DF_Engagement_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            BIGINT                  NULL,

    CONSTRAINT [PK_Engagement] PRIMARY KEY CLUSTERED ([EngagementId]),
    CONSTRAINT [FK_Engagement_TimeSlot] FOREIGN KEY ([TimeSlotId]) REFERENCES [venue].[TimeSlot]([TimeSlotId]),
    CONSTRAINT [FK_Engagement_Artist] FOREIGN KEY ([ArtistId]) REFERENCES [core].[Artist]([ArtistId]),
    CONSTRAINT [FK_Engagement_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_Engagement_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [identity].[User]([UserId])
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

-- Unique constraint: prevents duplicate artist assignments to the same time slot
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Engagement_TimeSlotId_ArtistId]
    ON [schedule].[Engagement]([TimeSlotId], [ArtistId])
    WHERE [IsDeleted] = 0;
GO
