-- =======================================================
-- Table: core.FestivalEdition
-- Description: Specific instances of a festival with dates and timezone
-- =======================================================
CREATE TABLE [core].[FestivalEdition]
(
    [EditionId]             UNIQUEIDENTIFIER    NOT NULL,
    [FestivalId]            UNIQUEIDENTIFIER    NOT NULL,
    [Name]                  NVARCHAR(200)       NOT NULL,
    [StartDateUtc]          DATETIME2(7)        NOT NULL,
    [EndDateUtc]            DATETIME2(7)        NOT NULL,
    [TimezoneId]            NVARCHAR(100)       NOT NULL,
    [TicketUrl]             NVARCHAR(2000)      NULL,
    [Status]                TINYINT             NOT NULL    CONSTRAINT [DF_FestivalEdition_Status] DEFAULT (0), -- 0 = Draft, 1 = Published, 2 = Archived
    [IsDeleted]             BIT                 NOT NULL    CONSTRAINT [DF_FestivalEdition_IsDeleted] DEFAULT (0),
    [DeletedAtUtc]          DATETIME2(7)        NULL,
    [CreatedAtUtc]          DATETIME2(7)        NOT NULL    CONSTRAINT [DF_FestivalEdition_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             UNIQUEIDENTIFIER    NULL,
    [ModifiedAtUtc]         DATETIME2(7)        NOT NULL    CONSTRAINT [DF_FestivalEdition_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            UNIQUEIDENTIFIER    NULL,

    CONSTRAINT [PK_FestivalEdition] PRIMARY KEY CLUSTERED ([EditionId]),
    CONSTRAINT [FK_FestivalEdition_Festival] FOREIGN KEY ([FestivalId]) REFERENCES [core].[Festival]([FestivalId]),
    CONSTRAINT [CK_FestivalEdition_Status] CHECK ([Status] IN (0, 1, 2)),
    CONSTRAINT [CK_FestivalEdition_Dates] CHECK ([EndDateUtc] > [StartDateUtc])
);
GO

-- Index for festival lookups
CREATE NONCLUSTERED INDEX [IX_FestivalEdition_FestivalId]
    ON [core].[FestivalEdition]([FestivalId])
    WHERE [IsDeleted] = 0;
GO

-- Index for active editions by date
CREATE NONCLUSTERED INDEX [IX_FestivalEdition_StartDateUtc]
    ON [core].[FestivalEdition]([StartDateUtc], [EndDateUtc])
    WHERE [IsDeleted] = 0 AND [Status] = 1;
GO

-- Index for status filtering
CREATE NONCLUSTERED INDEX [IX_FestivalEdition_Status]
    ON [core].[FestivalEdition]([Status])
    WHERE [IsDeleted] = 0;
GO
