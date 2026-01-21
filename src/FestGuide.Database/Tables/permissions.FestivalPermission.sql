-- FestivalPermission table for user access control to festivals
CREATE TABLE [permissions].[FestivalPermission]
(
    [FestivalPermissionId]  UNIQUEIDENTIFIER    NOT NULL    CONSTRAINT [DF_FestivalPermission_FestivalPermissionId] DEFAULT (NEWSEQUENTIALID()),
    [FestivalId]            UNIQUEIDENTIFIER    NOT NULL,
    [UserId]                UNIQUEIDENTIFIER    NOT NULL,
    [Role]                  INT                 NOT NULL,   -- 0=Viewer, 1=Manager, 2=Administrator, 3=Owner
    [Scope]                 INT                 NOT NULL    CONSTRAINT [DF_FestivalPermission_Scope] DEFAULT (0),   -- 0=All, 1=Venues, 2=Schedule, 3=Artists, 4=Editions, 5=Integrations
    [InvitedByUserId]       UNIQUEIDENTIFIER    NULL,
    [AcceptedAtUtc]         DATETIME2(7)        NULL,
    [IsPending]             BIT                 NOT NULL    CONSTRAINT [DF_FestivalPermission_IsPending] DEFAULT (0),
    [IsRevoked]             BIT                 NOT NULL    CONSTRAINT [DF_FestivalPermission_IsRevoked] DEFAULT (0),
    [RevokedAtUtc]          DATETIME2(7)        NULL,
    [CreatedAtUtc]          DATETIME2(7)        NOT NULL    CONSTRAINT [DF_FestivalPermission_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             UNIQUEIDENTIFIER    NULL,
    [ModifiedAtUtc]         DATETIME2(7)        NOT NULL    CONSTRAINT [DF_FestivalPermission_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            UNIQUEIDENTIFIER    NULL,

    CONSTRAINT [PK_FestivalPermission] PRIMARY KEY CLUSTERED ([FestivalPermissionId]),
    CONSTRAINT [FK_FestivalPermission_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User] ([UserId]),
    CONSTRAINT [FK_FestivalPermission_InvitedBy] FOREIGN KEY ([InvitedByUserId]) REFERENCES [identity].[User] ([UserId]),
    -- Note: FK to core.Festival will be added in Phase 2 when Festival table is created
    CONSTRAINT [CK_FestivalPermission_Role] CHECK ([Role] >= 0 AND [Role] <= 3),
    CONSTRAINT [CK_FestivalPermission_Scope] CHECK ([Scope] >= 0 AND [Scope] <= 5)
);
GO

-- Unique constraint: One permission per user per festival
CREATE UNIQUE NONCLUSTERED INDEX [UQ_FestivalPermission_UserFestival]
ON [permissions].[FestivalPermission] ([UserId], [FestivalId])
WHERE ([IsRevoked] = 0);
GO

-- Index for festival lookups
CREATE NONCLUSTERED INDEX [IX_FestivalPermission_FestivalId]
ON [permissions].[FestivalPermission] ([FestivalId])
INCLUDE ([UserId], [Role], [Scope], [IsPending], [IsRevoked]);
GO

-- Index for user lookups
CREATE NONCLUSTERED INDEX [IX_FestivalPermission_UserId]
ON [permissions].[FestivalPermission] ([UserId])
WHERE ([IsRevoked] = 0);
GO

-- Index for finding owner
CREATE NONCLUSTERED INDEX [IX_FestivalPermission_Owner]
ON [permissions].[FestivalPermission] ([FestivalId], [Role])
WHERE ([Role] = 3 AND [IsRevoked] = 0);
GO
