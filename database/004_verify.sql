-- Non-destructive transactional verification. Requires 001_create.sql.
USE TaskManager;
GO
SET XACT_ABORT OFF;
BEGIN TRANSACTION;
INSERT dbo.Users(Name, Email) VALUES(N'Verificación', CONCAT(NEWID(), N'@example.com'));
DECLARE @UserId INT = SCOPE_IDENTITY();
INSERT dbo.Tasks(Title, UserId, AdditionalData) VALUES(N'Verificar reglas', @UserId, N'{"priority":"High","tags":["test"]}');
DECLARE @TaskId INT = SCOPE_IDENTITY();
IF NOT EXISTS (SELECT 1 FROM dbo.Tasks WHERE Id=@TaskId AND Status='Pending' AND JSON_VALUE(AdditionalData,'$.priority')='High')
    THROW 51001, 'Defaults or JSON query failed.', 1;
UPDATE dbo.Tasks SET Status='InProgress' WHERE Id=@TaskId;
UPDATE dbo.Tasks SET Status='Done' WHERE Id=@TaskId;
ROLLBACK;
GO
-- Each failing statement is separately rolled back by its own transaction.
BEGIN TRY
    BEGIN TRANSACTION;
    INSERT dbo.Users(Name, Email) VALUES(N'Verificación', CONCAT(NEWID(), N'@example.com'));
    DECLARE @UserId INT = SCOPE_IDENTITY();
    INSERT dbo.Tasks(Title, UserId) VALUES(N'Transición inválida', @UserId);
    DECLARE @TaskId INT = SCOPE_IDENTITY();
    UPDATE dbo.Tasks SET Status='Done' WHERE Id=@TaskId;
    ROLLBACK;
    THROW 51002, 'FAIL: Pending to Done was allowed.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() <> 51000 THROW;
    PRINT 'PASS: Pending to Done rejected.';
END CATCH;
GO
BEGIN TRY
    BEGIN TRANSACTION;
    INSERT dbo.Users(Name, Email) VALUES(N'Verificación', CONCAT(NEWID(), N'@example.com'));
    DECLARE @UserId INT = SCOPE_IDENTITY();
    INSERT dbo.Tasks(Title, UserId, AdditionalData) VALUES(N'JSON inválido', @UserId, N'not json');
    ROLLBACK;
    THROW 51003, 'FAIL: invalid JSON was allowed.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() <> 547 THROW;
    PRINT 'PASS: invalid JSON rejected.';
END CATCH;
GO
