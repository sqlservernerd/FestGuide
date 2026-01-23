-- =======================================================
-- Table: schedule.Schedule
-- Description: Master schedule for a festival edition
--              Supports versioning: max 2 per edition
--              (one published and one draft)
-- =======================================================
CREATE TABLE [schedule].[Schedule]
(
    [ScheduleId]            BIGINT IDENTITY(1,1)    NOT NULL,
    [EditionId]             BIGINT                  NOT NULL,
    [Version]               INT                     NOT NULL    CONSTRAINT [DF_Schedule_Version] DEFAULT (1),
    [PublishedAtUtc]        DATETIME2(7)            NULL,
    [PublishedBy]           BIGINT                  NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_Schedule_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             BIGINT                  NULL,
    [ModifiedAtUtc]         DATETIME2(7)            NOT NULL    CONSTRAINT [DF_Schedule_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            BIGINT                  NULL,

    CONSTRAINT [PK_Schedule] PRIMARY KEY CLUSTERED ([ScheduleId]),
    CONSTRAINT [FK_Schedule_Edition] FOREIGN KEY ([EditionId]) REFERENCES [core].[FestivalEdition]([EditionId]),
    CONSTRAINT [FK_Schedule_PublishedBy] FOREIGN KEY ([PublishedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_Schedule_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_Schedule_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [identity].[User]([UserId])
);
GO

-- Unique constraint: one published schedule per edition
-- Note: This constraint ensures only one published schedule exists per edition
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Schedule_EditionId_Published]
    ON [schedule].[Schedule]([EditionId])
    WHERE [PublishedAtUtc] IS NOT NULL;
GO

-- Unique constraint: one draft schedule per edition
-- Note: This constraint ensures only one draft (unpublished) schedule exists per edition
-- Combined with the published constraint above, this enforces max 2 schedules per edition
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Schedule_EditionId_Draft]
    ON [schedule].[Schedule]([EditionId])
    WHERE [PublishedAtUtc] IS NULL;
GO

-- Index for edition schedule lookups
CREATE NONCLUSTERED INDEX [IX_Schedule_EditionId]
    ON [schedule].[Schedule]([EditionId], [Version] DESC);
GO

-- Index for published schedules
CREATE NONCLUSTERED INDEX [IX_Schedule_PublishedAtUtc]
    ON [schedule].[Schedule]([PublishedAtUtc])
    WHERE [PublishedAtUtc] IS NOT NULL;
GO
