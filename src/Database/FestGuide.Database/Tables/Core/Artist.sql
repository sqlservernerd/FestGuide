-- =======================================================
-- Table: core.Artist
-- Description: Performers at a festival (scoped to festival, reusable across editions)
-- =======================================================
CREATE TABLE [core].[Artist]
(
    [ArtistId]              BIGINT IDENTITY(1,1)    NOT NULL,
    [FestivalId]            BIGINT                  NOT NULL,
    [Name]                  NVARCHAR(200)           NOT NULL,
    [Genre]                 NVARCHAR(100)           NULL,
    [Bio]                   NVARCHAR(MAX)           NULL,
    [ImageUrl]              NVARCHAR(2083)          NULL,
    [WebsiteUrl]            NVARCHAR(2083)          NULL,
    [SpotifyUrl]            NVARCHAR(2083)          NULL,
    [IsDeleted]             BIT                     NOT NULL    CONSTRAINT [DF_Artist_IsDeleted] DEFAULT (0),
    [DeletedAtUtc]          DATETIME2(7)            NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_Artist_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             BIGINT                  NULL,
    [ModifiedAtUtc]         DATETIME2(7)            NOT NULL    CONSTRAINT [DF_Artist_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            BIGINT                  NULL,

    CONSTRAINT [PK_Artist] PRIMARY KEY CLUSTERED ([ArtistId]),
    CONSTRAINT [FK_Artist_Festival] FOREIGN KEY ([FestivalId]) REFERENCES [core].[Festival]([FestivalId]),
    CONSTRAINT [FK_Artist_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_Artist_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [identity].[User]([UserId])
);
GO

-- Index for festival artist lookups
CREATE NONCLUSTERED INDEX [IX_Artist_FestivalId]
    ON [core].[Artist]([FestivalId])
    WHERE [IsDeleted] = 0;
GO

-- Index for name searches
CREATE NONCLUSTERED INDEX [IX_Artist_Name]
    ON [core].[Artist]([Name])
    WHERE [IsDeleted] = 0;
GO

-- Index for genre filtering
CREATE NONCLUSTERED INDEX [IX_Artist_Genre]
    ON [core].[Artist]([Genre])
    WHERE [IsDeleted] = 0 AND [Genre] IS NOT NULL;
GO
