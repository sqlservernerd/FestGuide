-- =======================================================
-- Table: venue.Stage
-- Description: Performance areas within a venue
-- =======================================================
CREATE TABLE [venue].[Stage]
(
    [StageId]               BIGINT IDENTITY(1,1)    NOT NULL,
    [VenueId]               BIGINT                  NOT NULL,
    [Name]                  NVARCHAR(200)           NOT NULL,
    [Description]           NVARCHAR(MAX)           NULL,
    [SortOrder]             INT                     NOT NULL    CONSTRAINT [DF_Stage_SortOrder] DEFAULT (0),
    [IsDeleted]             BIT                     NOT NULL    CONSTRAINT [DF_Stage_IsDeleted] DEFAULT (0),
    [DeletedAtUtc]          DATETIME2(7)            NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_Stage_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             BIGINT                  NULL,
    [ModifiedAtUtc]         DATETIME2(7)            NOT NULL    CONSTRAINT [DF_Stage_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            BIGINT                  NULL,

    CONSTRAINT [PK_Stage] PRIMARY KEY CLUSTERED ([StageId]),
    CONSTRAINT [FK_Stage_Venue] FOREIGN KEY ([VenueId]) REFERENCES [venue].[Venue]([VenueId]),
    CONSTRAINT [FK_Stage_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_Stage_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [identity].[User]([UserId])
);
GO

-- Index for venue stage lookups
CREATE NONCLUSTERED INDEX [IX_Stage_VenueId]
    ON [venue].[Stage]([VenueId])
    WHERE [IsDeleted] = 0;
GO

-- Index for ordering stages
CREATE NONCLUSTERED INDEX [IX_Stage_SortOrder]
    ON [venue].[Stage]([VenueId], [SortOrder])
    WHERE [IsDeleted] = 0;
GO
