-- =============================================================================
-- NICE Platform Collaboration Engine — Database Creation Script
-- Run against SQL Server 2019+ (or Azure SQL) as a user with db_owner rights.
-- All tables live in the [Collaboration] schema.
-- Example: Collaboration.Applications, Collaboration.Users, etc.
-- =============================================================================

USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'NICECollaborationEngine')
    CREATE DATABASE NICECollaborationEngine;
GO

USE NICECollaborationEngine;
GO

-- =============================================================================
-- Schema
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Collaboration')
BEGIN
    EXEC('CREATE SCHEMA [Collaboration]');
    PRINT 'Created schema [Collaboration]';
END
GO

-- =============================================================================
-- 1. Collaboration.Applications
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'Applications' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[Applications] (
        Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        Name                NVARCHAR(100)       NOT NULL,
        HashedApiKey        NVARCHAR(64)        NOT NULL,
        AuthProvider        NVARCHAR(20)        NOT NULL,
        MaxAgentsOnline     INT                 NOT NULL DEFAULT 0,
        MaxUsersOnline      INT                 NOT NULL DEFAULT 0,
        BlobContainerPath   NVARCHAR(200)       NULL,
        WebhookUrl          NVARCHAR(500)       NULL,
        IsActive            BIT                 NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT PK_Applications         PRIMARY KEY (Id),
        CONSTRAINT UQ_Applications_Name    UNIQUE (Name),
        CONSTRAINT UQ_Applications_ApiKey  UNIQUE (HashedApiKey)
    );
    PRINT 'Created Collaboration.Applications';
END
GO

-- =============================================================================
-- 2. Collaboration.ApplicationUserTypeConfigs
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'ApplicationUserTypeConfigs' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[ApplicationUserTypeConfigs] (
        Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        ApplicationId           UNIQUEIDENTIFIER    NOT NULL,
        UserType                NVARCHAR(30)        NOT NULL,
        ChatMode                NVARCHAR(50)        NULL,
        CanShareScreen          BIT                 NOT NULL DEFAULT 0,
        NeedScreenRecording     BIT                 NOT NULL DEFAULT 0,
        CanHandOffToOtherAgent  BIT                 NOT NULL DEFAULT 0,
        MaxParallelChats        INT                 NOT NULL DEFAULT 0,
        AutoRecordScreen        BIT                 NOT NULL DEFAULT 0,
        SupervisorCanWatchLive  BIT                 NOT NULL DEFAULT 0,

        CONSTRAINT PK_ApplicationUserTypeConfigs    PRIMARY KEY (Id),
        CONSTRAINT UQ_ApplicationUserTypeConfigs    UNIQUE (ApplicationId, UserType),
        CONSTRAINT FK_ApplicationUserTypeConfigs_App
            FOREIGN KEY (ApplicationId)
            REFERENCES [Collaboration].[Applications](Id) ON DELETE CASCADE
    );
    PRINT 'Created Collaboration.ApplicationUserTypeConfigs';
END
GO

-- =============================================================================
-- 3. Collaboration.Users
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'Users' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[Users] (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        ExternalUserId  NVARCHAR(256)       NOT NULL,
        FirstName       NVARCHAR(100)       NULL,
        LastName        NVARCHAR(100)       NULL,
        Email           NVARCHAR(256)       NOT NULL,
        IsActive        BIT                 NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT PK_Users         PRIMARY KEY (Id),
        CONSTRAINT UQ_Users_ExtId   UNIQUE (ExternalUserId)
    );
    CREATE INDEX IX_Users_Email ON [Collaboration].[Users](Email);
    PRINT 'Created Collaboration.Users';
END
GO

-- =============================================================================
-- 4. Collaboration.ApplicationUsers
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'ApplicationUsers' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[ApplicationUsers] (
        ApplicationId   UNIQUEIDENTIFIER    NOT NULL,
        UserId          UNIQUEIDENTIFIER    NOT NULL,
        Role            NVARCHAR(30)        NOT NULL,
        IsActive        BIT                 NOT NULL DEFAULT 1,
        AddedAt         DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT PK_ApplicationUsers PRIMARY KEY (ApplicationId, UserId),
        CONSTRAINT FK_ApplicationUsers_App
            FOREIGN KEY (ApplicationId) REFERENCES [Collaboration].[Applications](Id) ON DELETE CASCADE,
        CONSTRAINT FK_ApplicationUsers_User
            FOREIGN KEY (UserId)        REFERENCES [Collaboration].[Users](Id)         ON DELETE CASCADE
    );
    PRINT 'Created Collaboration.ApplicationUsers';
END
GO

-- =============================================================================
-- 5. Collaboration.CurrentSessions
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'CurrentSessions' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[CurrentSessions] (
        Id                      UNIQUEIDENTIFIER    NOT NULL,
        ApplicationId           UNIQUEIDENTIFIER    NOT NULL,
        UserId                  UNIQUEIDENTIFIER    NOT NULL,
        UserType                NVARCHAR(30)        NOT NULL,
        AuthProvider            NVARCHAR(20)        NOT NULL,
        SignalRConnectionId     NVARCHAR(256)       NULL,
        ConnectedAt             DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        LastSeenAt              DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        CurrentCollaborationId  UNIQUEIDENTIFIER    NULL,

        CONSTRAINT PK_CurrentSessions PRIMARY KEY (Id),
        CONSTRAINT FK_CurrentSessions_App
            FOREIGN KEY (ApplicationId) REFERENCES [Collaboration].[Applications](Id) ON DELETE CASCADE,
        CONSTRAINT FK_CurrentSessions_User
            FOREIGN KEY (UserId)        REFERENCES [Collaboration].[Users](Id)         ON DELETE CASCADE
    );
    CREATE INDEX IX_CurrentSessions_App
        ON [Collaboration].[CurrentSessions](ApplicationId);
    CREATE INDEX IX_CurrentSessions_AppType
        ON [Collaboration].[CurrentSessions](ApplicationId, UserType);
    PRINT 'Created Collaboration.CurrentSessions';
END
GO

-- =============================================================================
-- 6. Collaboration.UserSessions
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'UserSessions' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[UserSessions] (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        ApplicationId   UNIQUEIDENTIFIER    NOT NULL,
        UserId          UNIQUEIDENTIFIER    NOT NULL,
        UserType        NVARCHAR(30)        NOT NULL,
        AuthProvider    NVARCHAR(20)        NOT NULL,
        ConnectedAt     DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        EndedAt         DATETIME2           NULL,
        DurationSeconds INT                 NULL,
        EndReason       NVARCHAR(50)        NULL,

        CONSTRAINT PK_UserSessions PRIMARY KEY (Id),
        CONSTRAINT FK_UserSessions_App
            FOREIGN KEY (ApplicationId) REFERENCES [Collaboration].[Applications](Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserSessions_User
            FOREIGN KEY (UserId)        REFERENCES [Collaboration].[Users](Id)         ON DELETE CASCADE
    );
    CREATE INDEX IX_UserSessions_User        ON [Collaboration].[UserSessions](UserId);
    CREATE INDEX IX_UserSessions_App         ON [Collaboration].[UserSessions](ApplicationId);
    CREATE INDEX IX_UserSessions_ConnectedAt ON [Collaboration].[UserSessions](ConnectedAt);
    PRINT 'Created Collaboration.UserSessions';
END
GO

-- =============================================================================
-- 7. Collaboration.Collaborations
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'Collaborations' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[Collaborations] (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        ApplicationId   UNIQUEIDENTIFIER    NOT NULL,
        ExternalUserId  UNIQUEIDENTIFIER    NULL,
        Status          NVARCHAR(30)        NOT NULL DEFAULT 'Pending',
        ChatMode        NVARCHAR(50)        NOT NULL,
        IsScreenSharing BIT                 NOT NULL DEFAULT 0,
        IsRecorded      BIT                 NOT NULL DEFAULT 0,
        CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        EndedAt         DATETIME2           NULL,
        EndReason       NVARCHAR(50)        NULL,

        CONSTRAINT PK_Collaborations PRIMARY KEY (Id),
        CONSTRAINT FK_Collaborations_App
            FOREIGN KEY (ApplicationId)  REFERENCES [Collaboration].[Applications](Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Collaborations_ExternalUser
            FOREIGN KEY (ExternalUserId) REFERENCES [Collaboration].[Users](Id)         ON DELETE SET NULL
    );
    CREATE INDEX IX_Collaborations_App       ON [Collaboration].[Collaborations](ApplicationId);
    CREATE INDEX IX_Collaborations_Status    ON [Collaboration].[Collaborations](Status);
    CREATE INDEX IX_Collaborations_CreatedAt ON [Collaboration].[Collaborations](CreatedAt);
    PRINT 'Created Collaboration.Collaborations';
END
GO

-- =============================================================================
-- 8. Collaboration.Participants
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'Participants' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[Participants] (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        CollaborationId UNIQUEIDENTIFIER    NOT NULL,
        UserId          UNIQUEIDENTIFIER    NOT NULL,
        UserType        NVARCHAR(30)        NOT NULL,
        JoinedAt        DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        LeftAt          DATETIME2           NULL,
        IsActiveAgent   BIT                 NOT NULL DEFAULT 0,

        CONSTRAINT PK_Participants PRIMARY KEY (Id),
        CONSTRAINT FK_Participants_Collab
            FOREIGN KEY (CollaborationId) REFERENCES [Collaboration].[Collaborations](Id) ON DELETE CASCADE,
        CONSTRAINT FK_Participants_User
            FOREIGN KEY (UserId)          REFERENCES [Collaboration].[Users](Id)           ON DELETE NO ACTION
    );
    CREATE INDEX IX_Participants_Collab
        ON [Collaboration].[Participants](CollaborationId);
    CREATE INDEX IX_Participants_CollUser
        ON [Collaboration].[Participants](CollaborationId, UserId);
    PRINT 'Created Collaboration.Participants';
END
GO

-- =============================================================================
-- 9. Collaboration.Messages
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'Messages' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[Messages] (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        CollaborationId UNIQUEIDENTIFIER    NOT NULL,
        SenderId        UNIQUEIDENTIFIER    NOT NULL,
        SenderType      NVARCHAR(30)        NOT NULL,
        Body            NVARCHAR(4000)      NULL,
        MessageType     NVARCHAR(20)        NOT NULL DEFAULT 'Text',
        IsDeleted       BIT                 NOT NULL DEFAULT 0,
        SentAt          DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        ReadAt          DATETIME2           NULL,

        CONSTRAINT PK_Messages PRIMARY KEY (Id),
        CONSTRAINT FK_Messages_Collab
            FOREIGN KEY (CollaborationId) REFERENCES [Collaboration].[Collaborations](Id) ON DELETE CASCADE,
        CONSTRAINT FK_Messages_Sender
            FOREIGN KEY (SenderId)        REFERENCES [Collaboration].[Users](Id)           ON DELETE NO ACTION
    );
    CREATE INDEX IX_Messages_Collab ON [Collaboration].[Messages](CollaborationId);
    CREATE INDEX IX_Messages_SentAt ON [Collaboration].[Messages](SentAt);
    PRINT 'Created Collaboration.Messages';
END
GO

-- =============================================================================
-- 10. Collaboration.Attachments
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'Attachments' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[Attachments] (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        MessageId       UNIQUEIDENTIFIER    NOT NULL,
        FileName        NVARCHAR(260)       NOT NULL,
        ContentType     NVARCHAR(100)       NOT NULL,
        FileSizeBytes   BIGINT              NOT NULL,
        BlobUri         NVARCHAR(1000)      NOT NULL,
        ThumbnailUri    NVARCHAR(1000)      NULL,
        UploadedAt      DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT PK_Attachments PRIMARY KEY (Id),
        CONSTRAINT FK_Attachments_Message
            FOREIGN KEY (MessageId) REFERENCES [Collaboration].[Messages](Id) ON DELETE CASCADE
    );
    PRINT 'Created Collaboration.Attachments';
END
GO

-- =============================================================================
-- 11. Collaboration.BotMessages
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'BotMessages' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[BotMessages] (
        Id               UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        CollaborationId  UNIQUEIDENTIFIER    NOT NULL,
        Prompt           NVARCHAR(2000)      NULL,
        Response         NVARCHAR(4000)      NOT NULL,
        DetectedIntent   NVARCHAR(100)       NULL,
        ConfidenceScore  FLOAT               NULL,
        TriggeredHandOff BIT                 NOT NULL DEFAULT 0,
        SentAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT PK_BotMessages PRIMARY KEY (Id),
        CONSTRAINT FK_BotMessages_Collab
            FOREIGN KEY (CollaborationId) REFERENCES [Collaboration].[Collaborations](Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_BotMessages_Collab ON [Collaboration].[BotMessages](CollaborationId);
    PRINT 'Created Collaboration.BotMessages';
END
GO

-- =============================================================================
-- 12. Collaboration.Recordings
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'Recordings' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[Recordings] (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        CollaborationId UNIQUEIDENTIFIER    NOT NULL,
        RecordingType   NVARCHAR(20)        NOT NULL DEFAULT 'Screen',
        StartedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        StoppedAt       DATETIME2           NULL,
        DurationSeconds INT                 NULL,
        BlobUri         NVARCHAR(1000)      NULL,
        FileSizeBytes   BIGINT              NULL,
        Status          NVARCHAR(20)        NOT NULL DEFAULT 'Pending',

        CONSTRAINT PK_Recordings PRIMARY KEY (Id),
        CONSTRAINT FK_Recordings_Collab
            FOREIGN KEY (CollaborationId) REFERENCES [Collaboration].[Collaborations](Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Recordings_Collab ON [Collaboration].[Recordings](CollaborationId);
    CREATE INDEX IX_Recordings_Status ON [Collaboration].[Recordings](Status);
    PRINT 'Created Collaboration.Recordings';
END
GO

-- =============================================================================
-- 13. Collaboration.TransferRequests
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'TransferRequests' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[TransferRequests] (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        CollaborationId UNIQUEIDENTIFIER    NOT NULL,
        FromUserId      UNIQUEIDENTIFIER    NOT NULL,
        ToUserId        UNIQUEIDENTIFIER    NULL,
        ToQueue         NVARCHAR(100)       NULL,
        TransferNote    NVARCHAR(500)       NULL,
        Status          NVARCHAR(20)        NOT NULL DEFAULT 'Pending',
        RequestedAt     DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        RespondedAt     DATETIME2           NULL,

        CONSTRAINT PK_TransferRequests PRIMARY KEY (Id),
        CONSTRAINT FK_TransferRequests_Collab
            FOREIGN KEY (CollaborationId) REFERENCES [Collaboration].[Collaborations](Id)  ON DELETE CASCADE,
        CONSTRAINT FK_TransferRequests_FromUser
            FOREIGN KEY (FromUserId)      REFERENCES [Collaboration].[Users](Id)            ON DELETE NO ACTION,
        CONSTRAINT FK_TransferRequests_ToUser
            FOREIGN KEY (ToUserId)        REFERENCES [Collaboration].[Users](Id)            ON DELETE SET NULL
    );
    CREATE INDEX IX_TransferRequests_Collab ON [Collaboration].[TransferRequests](CollaborationId);
    CREATE INDEX IX_TransferRequests_Status ON [Collaboration].[TransferRequests](Status);
    PRINT 'Created Collaboration.TransferRequests';
END
GO

-- =============================================================================
-- 14. Collaboration.AuditLogs  (append-only — no FK cascades)
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'AuditLogs' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[AuditLogs] (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        CollaborationId UNIQUEIDENTIFIER    NULL,
        ApplicationId   UNIQUEIDENTIFIER    NULL,
        UserId          UNIQUEIDENTIFIER    NULL,
        Category        NVARCHAR(30)        NOT NULL,
        EventName       NVARCHAR(100)       NOT NULL,
        Payload         NVARCHAR(MAX)       NULL,
        IpAddress       NVARCHAR(45)        NULL,
        OccurredAt      DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT PK_AuditLogs PRIMARY KEY (Id)
        -- No FK constraints: audit rows must outlive the entities they reference
    );
    CREATE INDEX IX_AuditLogs_OccurredAt ON [Collaboration].[AuditLogs](OccurredAt);
    CREATE INDEX IX_AuditLogs_EventName  ON [Collaboration].[AuditLogs](EventName);
    CREATE INDEX IX_AuditLogs_CollabDate ON [Collaboration].[AuditLogs](CollaborationId, OccurredAt);
    CREATE INDEX IX_AuditLogs_AppDate    ON [Collaboration].[AuditLogs](ApplicationId,   OccurredAt);
    PRINT 'Created Collaboration.AuditLogs';
END
GO

-- =============================================================================
-- 15. Collaboration.WebhookNotifications
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'WebhookNotifications' AND s.name = 'Collaboration')
BEGIN
    CREATE TABLE [Collaboration].[WebhookNotifications] (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        ApplicationId   UNIQUEIDENTIFIER    NOT NULL,
        CollaborationId UNIQUEIDENTIFIER    NULL,
        EventType       NVARCHAR(100)       NOT NULL,
        Payload         NVARCHAR(MAX)       NOT NULL,
        WebhookUrl      NVARCHAR(500)       NOT NULL,
        Status          NVARCHAR(20)        NOT NULL DEFAULT 'Pending',
        HttpStatusCode  INT                 NULL,
        AttemptCount    INT                 NOT NULL DEFAULT 0,
        LastAttemptAt   DATETIME2           NULL,
        LastError       NVARCHAR(1000)      NULL,
        CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        DeliveredAt     DATETIME2           NULL,

        CONSTRAINT PK_WebhookNotifications PRIMARY KEY (Id),
        CONSTRAINT FK_WebhookNotifications_App
            FOREIGN KEY (ApplicationId) REFERENCES [Collaboration].[Applications](Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_WebhookNotifications_App       ON [Collaboration].[WebhookNotifications](ApplicationId);
    CREATE INDEX IX_WebhookNotifications_Status    ON [Collaboration].[WebhookNotifications](Status);
    CREATE INDEX IX_WebhookNotifications_CreatedAt ON [Collaboration].[WebhookNotifications](CreatedAt);
    PRINT 'Created Collaboration.WebhookNotifications';
END
GO

PRINT '=== All 15 tables created in [Collaboration] schema ===';
