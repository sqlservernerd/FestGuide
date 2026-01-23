-- =======================================================
-- Table: permissions.FestivalPermission
-- Description: User permissions for festival access
-- =======================================================
CREATE TABLE [permissions].[FestivalPermission]
(
    [FestivalPermissionId]  BIGINT IDENTITY(1,1)    NOT NULL,
    [FestivalId]            BIGINT                  NOT NULL,
    [UserId]                BIGINT                  NOT NULL,
    [Role]                  TINYINT                 NOT NULL,   -- 0 = Viewer, 1 = Manager, 2 = Administrator, 3 = Owner
    [Scope]                 TINYINT                 NOT NULL    CONSTRAINT [DF_FestivalPermission_Scope] DEFAULT (0), -- 0 = All, 1 = Venues, 2 = Schedule, 3 = Artists, 4 = Editions, 5 = Integrations
    [InvitedByUserId]       BIGINT                  NULL,
    [AcceptedAtUtc]         DATETIME2(7)            NULL,
    [IsPending]             BIT                     NOT NULL    CONSTRAINT [DF_FestivalPermission_IsPending] DEFAULT (1),
    [IsRevoked]             BIT                     NOT NULL    CONSTRAINT [DF_FestivalPermission_IsRevoked] DEFAULT (0),
    [RevokedAtUtc]          DATETIME2(7)            NULL,
    [RevokedBy]             BIGINT                  NULL,
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_FestivalPermission_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             BIGINT                  NULL,
    [ModifiedAtUtc]         DATETIME2(7)            NOT NULL    CONSTRAINT [DF_FestivalPermission_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            BIGINT                  NULL,

    CONSTRAINT [PK_FestivalPermission] PRIMARY KEY CLUSTERED ([FestivalPermissionId]),
    CONSTRAINT [FK_FestivalPermission_Festival] FOREIGN KEY ([FestivalId]) REFERENCES [core].[Festival]([FestivalId]),
    CONSTRAINT [FK_FestivalPermission_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_FestivalPermission_InvitedByUser] FOREIGN KEY ([InvitedByUserId]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_FestivalPermission_RevokedBy] FOREIGN KEY ([RevokedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_FestivalPermission_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_FestivalPermission_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [CK_FestivalPermission_Role] CHECK ([Role] IN (0, 1, 2, 3)),
    CONSTRAINT [CK_FestivalPermission_Scope] CHECK ([Scope] IN (0, 1, 2, 3, 4, 5))
);
GO

-- Index for user permission lookups
CREATE NONCLUSTERED INDEX [IX_FestivalPermission_UserId]
    ON [permissions].[FestivalPermission]([UserId])
    WHERE [IsRevoked] = 0;
GO

-- Index for festival permission lookups
CREATE NONCLUSTERED INDEX [IX_FestivalPermission_FestivalId]
    ON [permissions].[FestivalPermission]([FestivalId])
    WHERE [IsRevoked] = 0;
GO

-- Index for invited by user lookups
CREATE NONCLUSTERED INDEX [IX_FestivalPermission_InvitedByUserId]
    ON [permissions].[FestivalPermission]([InvitedByUserId])
    WHERE [InvitedByUserId] IS NOT NULL;
GO

-- Composite index for user + festival authorization checks
CREATE NONCLUSTERED INDEX [IX_FestivalPermission_UserId_FestivalId]
    ON [permissions].[FestivalPermission]([UserId], [FestivalId])
    WHERE [IsRevoked] = 0;
GO
