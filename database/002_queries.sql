-- DBeaver: ejecutar despues de crear las tablas y registrar datos desde la aplicacion o API.
-- Solo consultas. Cambiar los valores de ejemplo en cada SELECT segun los datos.
USE TaskManager;
-- Tareas del usuario 1 en estado Pending, ordenadas por fecha de creacion.
SELECT t.Id, t.Title, u.Name AS UserName, t.Status, t.CreatedAt,
       JSON_VALUE(t.AdditionalData, '$.priority') AS Priority,
       JSON_VALUE(t.AdditionalData, '$.estimatedCompletion') AS EstimatedCompletion,
       JSON_QUERY(t.AdditionalData, '$.tags') AS Tags
FROM dbo.Tasks t JOIN dbo.Users u ON u.Id = t.UserId
WHERE t.UserId = 1 AND t.Status = 'Pending'
ORDER BY t.CreatedAt DESC, t.Id DESC;
-- JSON validation and filtering.
SELECT Id, Title, AdditionalData FROM dbo.Tasks
WHERE ISJSON(AdditionalData) = 1 AND JSON_VALUE(AdditionalData, '$.priority') = N'High';
-- Expand array elements using OPENJSON.
SELECT t.Id, t.Title, tag.[value] AS Tag
FROM dbo.Tasks t CROSS APPLY OPENJSON(t.AdditionalData, '$.tags') tag;
-- Preview a modified JSON value without changing stored data.
SELECT Id, AdditionalData AS OriginalData,
       JSON_MODIFY(AdditionalData, '$.priority', N'High') AS ModifiedData
FROM dbo.Tasks
WHERE Id = 1;
