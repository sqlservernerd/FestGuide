-- =======================================================
-- Table: venue.EditionVenue
-- Description: Junction table linking editions to venues
-- =======================================================
CREATE TABLE [venue].[EditionVenue]
(
    [EditionVenueId]        BIGINT IDENTITY(1,1)    NOT NULL,
    [EditionId]             BIGINT                  NOT NULL,
    [VenueId]               BIGINT                  NOT NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_EditionVenue_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             BIGINT                  NULL,

    CONSTRAINT [PK_EditionVenue] PRIMARY KEY CLUSTERED ([EditionVenueId]),
    CONSTRAINT [FK_EditionVenue_Edition] FOREIGN KEY ([EditionId]) REFERENCES [core].[FestivalEdition]([EditionId]),
    CONSTRAINT [FK_EditionVenue_Venue] FOREIGN KEY ([VenueId]) REFERENCES [venue].[Venue]([VenueId]),
    CONSTRAINT [FK_EditionVenue_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[User]([UserId])
);
GO

-- Unique constraint: one link per edition-venue pair
CREATE UNIQUE NONCLUSTERED INDEX [UQ_EditionVenue_EditionId_VenueId]
    ON [venue].[EditionVenue]([EditionId], [VenueId]);
GO

-- Index for venue lookups
CREATE NONCLUSTERED INDEX [IX_EditionVenue_VenueId]
    ON [venue].[EditionVenue]([VenueId]);
GO
