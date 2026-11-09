-- DBeaver: seleccionar TaskManager como base activa, no master.
-- Seleccionar todo este archivo y ejecutarlo como una sola sentencia.
-- Crear el trigger una sola vez, despues de 001_create.sql.
CREATE TRIGGER dbo.TR_Tasks_StatusTransition ON dbo.Tasks AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON i.Id = d.Id
               WHERE d.Status = 'Pending' AND i.Status = 'Done')
        THROW 51000, 'A task cannot move directly from Pending to Done.', 1;
END;
