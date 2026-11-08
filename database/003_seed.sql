USE TaskManager;
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'ana@example.com')
INSERT dbo.Users (Name, Email) VALUES (N'Ana Torres', N'ana@example.com');
DECLARE @UserId INT = (SELECT Id FROM dbo.Users WHERE Email = N'ana@example.com');
IF NOT EXISTS (SELECT 1 FROM dbo.Tasks WHERE Title = N'Preparar entrega técnica' AND UserId = @UserId)
INSERT dbo.Tasks (Title, UserId, AdditionalData)
VALUES (N'Preparar entrega técnica', @UserId, N'{"priority":"High","estimatedCompletion":"2026-11-10","tags":["entrega","fullstack"],"metadata":{"source":"seed"}}');
