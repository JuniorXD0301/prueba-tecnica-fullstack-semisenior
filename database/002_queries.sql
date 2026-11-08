USE TaskManager;
DECLARE @UserId INT = 1, @Status VARCHAR(20) = 'Pending', @Priority NVARCHAR(20) = N'High';
-- Required query: assigned user + optional status + creation date ordering.
SELECT t.Id, t.Title, u.Name AS UserName, t.Status, t.CreatedAt,
       JSON_VALUE(t.AdditionalData, '$.priority') AS Priority,
       JSON_VALUE(t.AdditionalData, '$.estimatedCompletion') AS EstimatedCompletion,
       JSON_QUERY(t.AdditionalData, '$.tags') AS Tags
FROM dbo.Tasks t JOIN dbo.Users u ON u.Id = t.UserId
WHERE t.UserId = @UserId AND (@Status IS NULL OR t.Status = @Status)
ORDER BY t.CreatedAt DESC, t.Id DESC;
-- JSON validation and filtering.
SELECT Id, Title, AdditionalData FROM dbo.Tasks
WHERE ISJSON(AdditionalData) = 1 AND JSON_VALUE(AdditionalData, '$.priority') = @Priority;
-- Expand array elements using OPENJSON.
SELECT t.Id, t.Title, tag.[value] AS Tag
FROM dbo.Tasks t CROSS APPLY OPENJSON(t.AdditionalData, '$.tags') tag;
-- Preview a modified JSON value without changing stored data.
SELECT Id, AdditionalData AS OriginalData,
       JSON_MODIFY(AdditionalData, '$.priority', N'High') AS ModifiedData
FROM dbo.Tasks
WHERE Id = 1;
