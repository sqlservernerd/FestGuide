-- =======================================================
-- Table: audit.AuditLog
-- Description: Tracks changes to data for compliance (SEC-011)
-- =======================================================
CREATE TABLE [audit].[AuditLog]
(
    [AuditLogId]            BIGINT IDENTITY(1,1)    NOT NULL,
    [TableSchema]           NVARCHAR(128)           NOT NULL,
    [TableName]             NVARCHAR(128)           NOT NULL,
    [RecordId]              BIGINT                  NOT NULL,
    [Action]                NVARCHAR(10)            NOT NULL,   -- 'INSERT', 'UPDATE', 'DELETE'
    [OldValues]             NVARCHAR(MAX)           NULL,       -- JSON of old values (for UPDATE/DELETE)
    [NewValues]             NVARCHAR(MAX)           NULL,       -- JSON of new values (for INSERT/UPDATE)
    [ChangedColumns]        NVARCHAR(MAX)           NULL,       -- JSON array of changed column names (for UPDATE)
    [UserId]                BIGINT                  NULL,
    [UserEmail]             NVARCHAR(256)           NULL,       -- Denormalized for audit independence
    [IpAddress]             NVARCHAR(45)            NULL,
    [UserAgent]             NVARCHAR(500)           NULL,
    [RequestId]             NVARCHAR(100)           NULL,       -- Correlation ID for request tracing
    [CreatedAtUtc]          DATETIME2(7)            NOT NULL    CONSTRAINT [DF_AuditLog_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([AuditLogId]),
    CONSTRAINT [CK_AuditLog_Action] CHECK ([Action] IN ('INSERT', 'UPDATE', 'DELETE'))
);
GO

-- Index for table/record lookups (view history of a specific record)
CREATE NONCLUSTERED INDEX [IX_AuditLog_TableName_RecordId]
    ON [audit].[AuditLog]([TableSchema], [TableName], [RecordId], [CreatedAtUtc] DESC);
GO

-- Index for user activity lookups
CREATE NONCLUSTERED INDEX [IX_AuditLog_UserId]
    ON [audit].[AuditLog]([UserId], [CreatedAtUtc] DESC)
    WHERE [UserId] IS NOT NULL;
GO

-- Index for time-based queries (compliance reporting)
CREATE NONCLUSTERED INDEX [IX_AuditLog_CreatedAtUtc]
    ON [audit].[AuditLog]([CreatedAtUtc] DESC)
    INCLUDE ([TableSchema], [TableName], [Action], [UserId]);
GO

-- Index for request correlation
CREATE NONCLUSTERED INDEX [IX_AuditLog_RequestId]
    ON [audit].[AuditLog]([RequestId])
    WHERE [RequestId] IS NOT NULL;
GO
