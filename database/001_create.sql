-- SQL Server 2016+ (compatibility level 130 or higher).
-- Run using SSMS or sqlcmd, with an account allowed to create databases.
IF DB_ID(N'TaskManager') IS NULL CREATE DATABASE TaskManager;
GO
USE TaskManager;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL,
    Email NVARCHAR(254) NOT NULL,
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
IF OBJECT_ID(N'dbo.Tasks', N'U') IS NULL
CREATE TABLE dbo.Tasks (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tasks PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    UserId INT NOT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Tasks_Status DEFAULT 'Pending',
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Tasks_CreatedAt DEFAULT SYSUTCDATETIME(),
    AdditionalData NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Tasks_AdditionalData DEFAULT N'{}',
    CONSTRAINT FK_Tasks_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Tasks_Title CHECK (LEN(LTRIM(RTRIM(Title))) > 0),
    CONSTRAINT CK_Tasks_Status CHECK (Status IN ('Pending','InProgress','Done')),
    CONSTRAINT CK_Tasks_Json CHECK (ISJSON(AdditionalData) = 1)
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tasks_UserId_Status_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Tasks'))
CREATE INDEX IX_Tasks_UserId_Status_CreatedAt ON dbo.Tasks(UserId, Status, CreatedAt DESC);
COMMIT;
GO
-- Protect the business rule even when updates bypass the API; supports multirow updates.
CREATE OR ALTER TRIGGER dbo.TR_Tasks_StatusTransition ON dbo.Tasks AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON i.Id = d.Id
               WHERE d.Status = 'Pending' AND i.Status = 'Done')
        THROW 51000, 'A task cannot move directly from Pending to Done.', 1;
END;
GO
