-- =======================================================
-- Table: identity.User
-- Description: User accounts in the system
-- =======================================================
CREATE TABLE [identity].[User]
(
    [UserId]                UNIQUEIDENTIFIER    NOT NULL,
    [Email]                 NVARCHAR(256)       NOT NULL,
    [EmailNormalized]       NVARCHAR(256)       NOT NULL,
    [EmailVerified]         BIT                 NOT NULL    CONSTRAINT [DF_User_EmailVerified] DEFAULT (0),
    [PasswordHash]          NVARCHAR(500)       NOT NULL,
    [DisplayName]           NVARCHAR(100)       NOT NULL,
    [UserType]              TINYINT             NOT NULL,   -- 0 = Attendee, 1 = Organizer
    [PreferredTimezoneId]   NVARCHAR(100)       NULL,
    [FailedLoginAttempts]   INT                 NOT NULL    CONSTRAINT [DF_User_FailedLoginAttempts] DEFAULT (0),
    [LockoutEndUtc]         DATETIME2(7)        NULL,
    [IsDeleted]             BIT                 NOT NULL    CONSTRAINT [DF_User_IsDeleted] DEFAULT (0),
    [DeletedAtUtc]          DATETIME2(7)        NULL,
    [CreatedAtUtc]          DATETIME2(7)        NOT NULL    CONSTRAINT [DF_User_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]             UNIQUEIDENTIFIER    NULL,
    [ModifiedAtUtc]         DATETIME2(7)        NOT NULL    CONSTRAINT [DF_User_ModifiedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [ModifiedBy]            UNIQUEIDENTIFIER    NULL,

    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([UserId])
);
GO

-- Unique constraint: one active email per user (allows reuse of deleted user emails)
CREATE UNIQUE NONCLUSTERED INDEX [UQ_User_EmailNormalized]
    ON [identity].[User]([EmailNormalized])
    WHERE [IsDeleted] = 0;
GO

-- Index for login lookups
CREATE NONCLUSTERED INDEX [IX_User_EmailNormalized]
    ON [identity].[User]([EmailNormalized])
    WHERE [IsDeleted] = 0;
GO

-- Index for filtering active users
CREATE NONCLUSTERED INDEX [IX_User_IsDeleted]
    ON [identity].[User]([IsDeleted])
    INCLUDE ([UserId], [Email], [DisplayName], [UserType]);
GO
