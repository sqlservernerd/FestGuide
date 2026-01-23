-- =======================================================
-- Table: attendee.PersonalSchedule
-- Description: Attendee's personal schedule for a festival edition
-- =======================================================
CREATE TABLE [attendee].[PersonalSchedule]
(
    [PersonalScheduleId]    BIGINT IDENTITY(1,1)    NOT NULL,
    [UserId]                BIGINT                  NOT NULL,
    [EditionId]             BIGINT                  NOT NULL,
    [Name]                  NVARCHAR(100)           NULL,
    [IsDefault]             BIT                     NOT NULL    CONSTRAINT [DF_PersonalSchedule_IsDefault] DEFAULT (1),
    [LastSyncedAtUtc]       DATETIME2(7)            NULL,
    [IsDeleted]             BIT                     NOT NULL    CONSTRAINT [DF_PersonalSchedule_IsDeleted] DEFAULT (0),
    [DeletedAtUtc]          DATETIME2(7)            NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_PersonalSchedule_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             BIGINT                  NULL,
    [ModifiedAtUtc]         DATETIME2(7)            NOT NULL    CONSTRAINT [DF_PersonalSchedule_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            BIGINT                  NULL,

    CONSTRAINT [PK_PersonalSchedule] PRIMARY KEY CLUSTERED ([PersonalScheduleId]),
    CONSTRAINT [FK_PersonalSchedule_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_PersonalSchedule_Edition] FOREIGN KEY ([EditionId]) REFERENCES [core].[FestivalEdition]([EditionId]),
    CONSTRAINT [FK_PersonalSchedule_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_PersonalSchedule_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [identity].[User]([UserId])
);
GO

-- Index for user schedule lookups
CREATE NONCLUSTERED INDEX [IX_PersonalSchedule_UserId]
    ON [attendee].[PersonalSchedule]([UserId])
    WHERE [IsDeleted] = 0;
GO

-- Index for edition schedule lookups
CREATE NONCLUSTERED INDEX [IX_PersonalSchedule_EditionId]
    ON [attendee].[PersonalSchedule]([EditionId])
    WHERE [IsDeleted] = 0;
GO

-- Unique constraint: one default schedule per user per edition
CREATE UNIQUE NONCLUSTERED INDEX [UQ_PersonalSchedule_UserId_EditionId_IsDefault]
    ON [attendee].[PersonalSchedule]([UserId], [EditionId])
    WHERE [IsDeleted] = 0 AND [IsDefault] = 1;
GO
