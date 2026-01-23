-- =======================================================
-- Table: venue.Venue
-- Description: Physical locations where festival events take place
-- =======================================================
CREATE TABLE [venue].[Venue]
(
    [VenueId]               UNIQUEIDENTIFIER    NOT NULL,
    [FestivalId]            UNIQUEIDENTIFIER    NOT NULL,
    [Name]                  NVARCHAR(200)       NOT NULL,
    [Description]           NVARCHAR(MAX)       NULL,
    [Address]               NVARCHAR(500)       NULL,
    [Latitude]              DECIMAL(10, 7)      NULL,
    [Longitude]             DECIMAL(10, 7)      NULL,
    [IsDeleted]             BIT                 NOT NULL    CONSTRAINT [DF_Venue_IsDeleted] DEFAULT (0),
    [DeletedAtUtc]          DATETIME2(7)        NULL,
    [CreatedAtUtc]          DATETIME2(7)        NOT NULL    CONSTRAINT [DF_Venue_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             UNIQUEIDENTIFIER    NULL,
    [ModifiedAtUtc]         DATETIME2(7)        NOT NULL    CONSTRAINT [DF_Venue_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            UNIQUEIDENTIFIER    NULL,

    CONSTRAINT [PK_Venue] PRIMARY KEY CLUSTERED ([VenueId]),
    CONSTRAINT [FK_Venue_Festival] FOREIGN KEY ([FestivalId]) REFERENCES [core].[Festival]([FestivalId]),
    CONSTRAINT [CK_Venue_Latitude] CHECK ([Latitude] IS NULL OR ([Latitude] >= -90 AND [Latitude] <= 90)),
    CONSTRAINT [CK_Venue_Longitude] CHECK ([Longitude] IS NULL OR ([Longitude] >= -180 AND [Longitude] <= 180))
);
GO

-- Index for festival venue lookups
CREATE NONCLUSTERED INDEX [IX_Venue_FestivalId]
    ON [venue].[Venue]([FestivalId])
    WHERE [IsDeleted] = 0;
GO
