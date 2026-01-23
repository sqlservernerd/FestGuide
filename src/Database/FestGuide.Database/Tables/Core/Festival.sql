-- =======================================================
-- Table: core.Festival
-- Description: Recurring festival brands
-- =======================================================
CREATE TABLE [core].[Festival]
(
    [FestivalId]            BIGINT IDENTITY(1,1)    NOT NULL,
    [Name]                  NVARCHAR(200)           NOT NULL,
    [Description]           NVARCHAR(MAX)           NULL,
    [ImageUrl]              NVARCHAR(2083)          NULL,
    [WebsiteUrl]            NVARCHAR(2083)          NULL,
    [OwnerUserId]           BIGINT                  NOT NULL,
    [IsDeleted]             BIT                     NOT NULL    CONSTRAINT [DF_Festival_IsDeleted] DEFAULT (0),
    [DeletedAtUtc]          DATETIME2(7)            NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_Festival_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             BIGINT                  NULL,
    [ModifiedAtUtc]         DATETIME2(7)            NOT NULL    CONSTRAINT [DF_Festival_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            BIGINT                  NULL,

    CONSTRAINT [PK_Festival] PRIMARY KEY CLUSTERED ([FestivalId]),
    CONSTRAINT [FK_Festival_OwnerUser] FOREIGN KEY ([OwnerUserId]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_Festival_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_Festival_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [identity].[User]([UserId])
);
GO

-- Index for owner lookups
CREATE NONCLUSTERED INDEX [IX_Festival_OwnerUserId]
    ON [core].[Festival]([OwnerUserId])
    WHERE [IsDeleted] = 0;
GO

-- Index for active festivals
CREATE NONCLUSTERED INDEX [IX_Festival_IsDeleted]
    ON [core].[Festival]([IsDeleted])
    INCLUDE ([FestivalId], [Name], [OwnerUserId]);
GO

-- Full-text search support (name)
CREATE NONCLUSTERED INDEX [IX_Festival_Name]
    ON [core].[Festival]([Name])
    WHERE [IsDeleted] = 0;
GO
