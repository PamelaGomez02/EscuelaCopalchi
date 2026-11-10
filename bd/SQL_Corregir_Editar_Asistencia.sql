USE AulaVirtualCopalchi;
GO
CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Obtener
    @id_asistencia INT,
    @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.id_asistencia, a.id_estudiante, a.id_grupo, a.fecha,
           a.estado, a.observacion,
           g.nombre AS grupo,
           CONCAT(e.nombre, ' ', e.apellido1, ' ', COALESCE(e.apellido2,'')) AS estudiante
    FROM dbo.ASISTENCIA AS a
    INNER JOIN dbo.ESTUDIANTE AS e ON e.id_estudiante = a.id_estudiante
    INNER JOIN dbo.GRUPO AS g ON g.id_grupo = a.id_grupo
    WHERE a.id_asistencia = @id_asistencia
      AND (@id_docente = 0 OR a.id_docente = @id_docente);
END;
GO
CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Modificar
    @id_usuario INT,
    @id_docente INT,
    @id_asistencia INT,
    @estado NVARCHAR(30),
    @hora NVARCHAR(20) = NULL,
    @observacion NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @estado NOT IN (N'Presente', N'Ausente', N'Tardanza', N'Ausente justificado', N'Justificado')
        THROW 50001, 'Estado de asistencia no válido.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.ASISTENCIA
                   WHERE id_asistencia = @id_asistencia
                     AND (@id_docente = 0 OR id_docente = @id_docente))
        THROW 50002, 'Registro inexistente o fuera del alcance del docente.', 1;
    -- La tabla ASISTENCIA mostrada no contiene columna de hora.
    -- La hora se recibe para mantener la firma del repositorio, pero no se guarda.
    UPDATE dbo.ASISTENCIA
    SET estado = @estado,
        observacion = NULLIF(LTRIM(RTRIM(@observacion)), '')
    WHERE id_asistencia = @id_asistencia
      AND (@id_docente = 0 OR id_docente = @id_docente);
END;
GO
-- Verificar firmas
SELECT p.name AS procedimiento, prm.name AS parametro
FROM sys.procedures p JOIN sys.parameters prm ON prm.object_id=p.object_id
WHERE p.name IN ('SP_AS_Asistencia_Obtener','SP_AS_Asistencia_Modificar')
ORDER BY p.name, prm.parameter_id;
