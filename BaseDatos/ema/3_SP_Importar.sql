/* Gestión de Notas - apoyo para importar desde Excel */

USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GN_Estudiante_Buscar
    @id_grupo INT = 0   -- 0 = todos los estudiantes; >0 = solo matriculados activos del grupo
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.id_estudiante, e.identificacion
    FROM dbo.ESTUDIANTE e
    WHERE @id_grupo = 0
       OR EXISTS (SELECT 1 FROM dbo.MATRICULA m
                  WHERE m.id_estudiante = e.id_estudiante
                    AND m.id_grupo = @id_grupo AND m.estado = N'Activa');
END
GO
