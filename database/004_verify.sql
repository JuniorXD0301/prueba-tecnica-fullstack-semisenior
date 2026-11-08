-- Requires 001_create.sql. Run only against a development database.
-- Test rows are committed individually and explicitly removed on success or failure.
USE TaskManager;

DECLARE @VerificationUserId INT;
DECLARE @VerificationTaskId INT;
DECLARE @TransitionRejected BIT = 0;
DECLARE @InvalidJsonRejected BIT = 0;

BEGIN TRY
    INSERT dbo.Users(Name, Email)
    VALUES(N'Verification', CONCAT(NEWID(), N'@example.com'));
    SET @VerificationUserId = SCOPE_IDENTITY();

    INSERT dbo.Tasks(Title, UserId, AdditionalData)
    VALUES(N'Valid transition', @VerificationUserId, N'{"priority":"High","tags":["test"]}');
    SET @VerificationTaskId = SCOPE_IDENTITY();

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Tasks
        WHERE Id = @VerificationTaskId AND Status = 'Pending'
          AND JSON_VALUE(AdditionalData, '$.priority') = 'High'
    )
        THROW 51001, 'FAIL: defaults or JSON query.', 1;

    UPDATE dbo.Tasks SET Status = 'InProgress' WHERE Id = @VerificationTaskId;
    UPDATE dbo.Tasks SET Status = 'Done' WHERE Id = @VerificationTaskId;
    IF NOT EXISTS (SELECT 1 FROM dbo.Tasks WHERE Id = @VerificationTaskId AND Status = 'Done')
        THROW 51004, 'FAIL: valid transition.', 1;
    PRINT 'PASS: defaults, JSON query and valid transition.';

    INSERT dbo.Tasks(Title, UserId) VALUES(N'Invalid transition', @VerificationUserId);
    SET @VerificationTaskId = SCOPE_IDENTITY();
    BEGIN TRY
        UPDATE dbo.Tasks SET Status = 'Done' WHERE Id = @VerificationTaskId;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() <> 51000 THROW;
        SET @TransitionRejected = 1;
    END CATCH;
    IF @TransitionRejected = 0
        THROW 51002, 'FAIL: Pending to Done was allowed.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Tasks WHERE Id = @VerificationTaskId AND Status = 'Pending')
        THROW 51005, 'FAIL: rejected transition changed the stored state.', 1;
    PRINT 'PASS: Pending to Done rejected; stored state preserved.';

    BEGIN TRY
        INSERT dbo.Tasks(Title, UserId, AdditionalData)
        VALUES(N'Invalid JSON', @VerificationUserId, N'not json');
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() <> 547 THROW;
        SET @InvalidJsonRejected = 1;
    END CATCH;
    IF @InvalidJsonRejected = 0
        THROW 51003, 'FAIL: invalid JSON was allowed.', 1;
    PRINT 'PASS: invalid JSON rejected.';

    DELETE FROM dbo.Tasks WHERE UserId = @VerificationUserId;
    DELETE FROM dbo.Users WHERE Id = @VerificationUserId;
END TRY
BEGIN CATCH
    -- Remove only records created by this execution, then preserve the original error.
    DELETE FROM dbo.Tasks WHERE UserId = @VerificationUserId;
    DELETE FROM dbo.Users WHERE Id = @VerificationUserId;
    THROW;
END CATCH;
