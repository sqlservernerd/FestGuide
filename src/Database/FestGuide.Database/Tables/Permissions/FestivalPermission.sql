-- =======================================================
-- Table: permissions.FestivalPermission
-- Description: User permissions for festival access
-- =======================================================
CREATE TABLE [permissions].[FestivalPermission]
(
    [FestivalPermissionId]  UNIQUEIDENTIFIER    NOT NULL,
    [FestivalId]            UNIQUEIDENTIFIER    NOT NULL,
    [UserId]                UNIQUEIDENTIFIER    NOT NULL,
    [Role]                  TINYINT             NOT NULL,   -- 0 = Viewer, 1 = Manager, 2 = Administrator, 3 = Owner
    [Scope]                 TINYINT             NOT NULL    CONSTRAINT [DF_FestivalPermission_Scope] DEFAULT (0), -- 0 = All, 1 = Venues, 2 = Schedule, 3 = Artists, 4 = Editions, 5 = Integrations
    [InvitedByUserId]       UNIQUEIDENTIFIER    NULL,
    [AcceptedAtUtc]         DATETIME2(7)        NULL,
    [IsPending]             BIT                 NOT NULL    CONSTRAINT [DF_FestivalPermission_IsPending] DEFAULT (1),
    [IsRevoked]             BIT                 NOT NULL    CONSTRAINT [DF_FestivalPermission_IsRevoked] DEFAULT (0),
    [RevokedAtUtc]          DATETIME2(7)        NULL,
    [CreatedAtUtc]          DATETIME2(7)        NOT NULL    CONSTRAINT [DF_FestivalPermission_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             UNIQUEIDENTIFIER    NULL,
    [ModifiedAtUtc]         DATETIME2(7)        NOT NULL    CONSTRAINT [DF_FestivalPermission_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            UNIQUEIDENTIFIER    NULL,

    CONSTRAINT [PK_FestivalPermission] PRIMARY KEY CLUSTERED ([FestivalPermissionId]),
    CONSTRAINT [FK_FestivalPermission_Festival] FOREIGN KEY ([FestivalId]) REFERENCES [core].[Festival]([FestivalId]),
    CONSTRAINT [FK_FestivalPermission_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([UserId]),
    CONSTRAINT [FK_FestivalPermission_InvitedByUser] FOREIGN KEY ([InvitedByUserId]) REFERENCES [identity].[User]([UserId]),
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
