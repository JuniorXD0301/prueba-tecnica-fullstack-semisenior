-- DBeaver: ejecutar despues de 000_create_database.sql.
-- Crear las tablas una sola vez; USE cambia el contexto de master a TaskManager.
USE TaskManager;

CREATE TABLE dbo.Users (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL,
    Email NVARCHAR(254) NOT NULL,
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
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
CREATE INDEX IX_Tasks_UserId_Status_CreatedAt ON dbo.Tasks(UserId, Status, CreatedAt DESC);
