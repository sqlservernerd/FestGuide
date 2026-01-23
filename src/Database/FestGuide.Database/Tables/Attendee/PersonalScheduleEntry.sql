-- =======================================================
-- Table: attendee.PersonalScheduleEntry
-- Description: Entries in an attendee's personal schedule
-- =======================================================
CREATE TABLE [attendee].[PersonalScheduleEntry]
(
    [PersonalScheduleEntryId]   BIGINT IDENTITY(1,1)    NOT NULL,
    [PersonalScheduleId]        BIGINT                  NOT NULL,
    [EngagementId]              BIGINT                  NOT NULL,
    [Notes]                     NVARCHAR(MAX)           NULL,
    [NotificationsEnabled]      BIT                     NOT NULL    CONSTRAINT [DF_PersonalScheduleEntry_NotificationsEnabled] DEFAULT (1),
    [IsDeleted]                 BIT                     NOT NULL    CONSTRAINT [DF_PersonalScheduleEntry_IsDeleted] DEFAULT (0),
    [DeletedAtUtc]              DATETIME2(7)            NULL,
    [CreatedAtUtc]              DATETIME2(7)            NOT NULL    CONSTRAINT [DF_PersonalScheduleEntry_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]                 BIGINT                  NULL,
    [ModifiedAtUtc]             DATETIME2(7)            NOT NULL    CONSTRAINT [DF_PersonalScheduleEntry_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]                BIGINT                  NULL,

    CONSTRAINT [PK_PersonalScheduleEntry] PRIMARY KEY CLUSTERED ([PersonalScheduleEntryId]),
    CONSTRAINT [FK_PersonalScheduleEntry_PersonalSchedule] FOREIGN KEY ([PersonalScheduleId]) REFERENCES [attendee].[PersonalSchedule]([PersonalScheduleId]),
    CONSTRAINT [FK_PersonalScheduleEntry_Engagement] FOREIGN KEY ([EngagementId]) REFERENCES [schedule].[Engagement]([EngagementId]),
    CONSTRAINT [FK_PersonalScheduleEntry_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_PersonalScheduleEntry_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [identity].[User]([UserId])
);
GO

-- Index for schedule entry lookups
CREATE NONCLUSTERED INDEX [IX_PersonalScheduleEntry_PersonalScheduleId]
    ON [attendee].[PersonalScheduleEntry]([PersonalScheduleId])
    WHERE [IsDeleted] = 0;
GO

-- Index for engagement entry lookups
CREATE NONCLUSTERED INDEX [IX_PersonalScheduleEntry_EngagementId]
    ON [attendee].[PersonalScheduleEntry]([EngagementId])
    WHERE [IsDeleted] = 0;
GO

-- Unique constraint: one entry per engagement per personal schedule
CREATE UNIQUE NONCLUSTERED INDEX [UQ_PersonalScheduleEntry_PersonalScheduleId_EngagementId]
    ON [attendee].[PersonalScheduleEntry]([PersonalScheduleId], [EngagementId])
    WHERE [IsDeleted] = 0;
GO
