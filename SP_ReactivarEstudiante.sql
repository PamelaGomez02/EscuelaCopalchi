-- Ejecutar en la base de datos de Aula Virtual Copalchí.
-- NO borra el historial ni limpia fecha_baja u observacion_baja.
CREATE OR ALTER PROCEDURE dbo.SP_ReactivarEstudiante
    @id_estudiante INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.ESTUDIANTE WHERE id_estudiante = @id_estudiante)
        THROW 50001, 'El estudiante no existe.', 1;

    IF EXISTS (SELECT 1 FROM dbo.ESTUDIANTE WHERE id_estudiante = @id_estudiante AND estado = 1)
        THROW 50002, 'El estudiante ya se encuentra activo.', 1;

    UPDATE dbo.ESTUDIANTE
       SET estado = 1
     WHERE id_estudiante = @id_estudiante AND estado = 0;
END;
GO
