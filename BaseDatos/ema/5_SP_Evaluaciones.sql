/* Gestión de notas - modificar y eliminar evaluaciones */
USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GN_Evaluacion_Actualizar
    @id_evaluacion INT, @nombre NVARCHAR(150), @porcentaje DECIMAL(5,2),
    @fecha DATE = NULL, @nota_maxima DECIMAL(6,2) = 100,
    @id_usuario INT, @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @id_grupo INT, @asignatura NVARCHAR(100), @trimestre TINYINT,
            @nombre_ant NVARCHAR(150), @pct_ant DECIMAL(5,2);
    SELECT @id_grupo = id_grupo, @asignatura = asignatura, @trimestre = trimestre,
           @nombre_ant = nombre, @pct_ant = porcentaje
    FROM dbo.EVALUACION
    WHERE id_evaluacion = @id_evaluacion AND estado = 1;
    IF @id_grupo IS NULL
        THROW 50025, N'La evaluación no existe.', 1;
    IF @id_docente > 0
       AND NOT EXISTS (SELECT 1 FROM dbo.GRUPO WHERE id_grupo = @id_grupo AND id_docente = @id_docente)
        THROW 50011, N'Solo el docente del grupo puede modificar evaluaciones.', 1;
    IF @porcentaje <= 0 OR @porcentaje > 100
        THROW 50023, N'El porcentaje debe ser mayor que 0 y no puede superar 100.', 1;
    IF @nota_maxima <= 0
        THROW 50024, N'La nota máxima debe ser mayor que 0.', 1;
    IF EXISTS (SELECT 1 FROM dbo.EVALUACION
               WHERE id_grupo = @id_grupo AND asignatura = @asignatura AND trimestre = @trimestre
                 AND estado = 1 AND nombre = @nombre AND id_evaluacion <> @id_evaluacion)
        THROW 50021, N'Ya existe una evaluación con ese nombre en la asignatura y el período.', 1;
    IF EXISTS (SELECT 1 FROM dbo.CALIFICACION WHERE id_evaluacion = @id_evaluacion AND nota > @nota_maxima)
        THROW 50026, N'Hay notas registradas mayores que la nueva nota máxima.', 1;
    DECLARE @suma_otras DECIMAL(6,2) =
        (SELECT ISNULL(SUM(porcentaje), 0) FROM dbo.EVALUACION
         WHERE id_grupo = @id_grupo AND asignatura = @asignatura AND trimestre = @trimestre
           AND estado = 1 AND id_evaluacion <> @id_evaluacion);
    IF @suma_otras + @porcentaje > 100
        THROW 50022, N'La suma de los porcentajes de la asignatura en este período no puede superar 100 %.', 1;
    UPDATE dbo.EVALUACION
    SET nombre = @nombre, porcentaje = @porcentaje, fecha = @fecha, nota_maxima = @nota_maxima
    WHERE id_evaluacion = @id_evaluacion;
    INSERT INTO dbo.AUDITORIA (id_usuario, accion, modulo, tabla_afectada, registro_afectado, descripcion)
    VALUES (@id_usuario, N'Modificar evaluación', N'Gestión de notas', N'EVALUACION',
            CAST(@id_evaluacion AS NVARCHAR(100)),
            CONCAT(@asignatura, N' - ', @nombre_ant, N' -> ', @nombre,
                   N' (', @pct_ant, N' % -> ', @porcentaje, N' %)'));
END
GO

-- Eliminación lógica: la evaluación y sus notas dejan de mostrarse y de contar en las notas finales.
CREATE OR ALTER PROCEDURE dbo.SP_GN_Evaluacion_Eliminar
    @id_evaluacion INT, @id_usuario INT, @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @id_grupo INT, @asignatura NVARCHAR(100), @nombre NVARCHAR(150);
    SELECT @id_grupo = id_grupo, @asignatura = asignatura, @nombre = nombre
    FROM dbo.EVALUACION
    WHERE id_evaluacion = @id_evaluacion AND estado = 1;
    IF @id_grupo IS NULL
        THROW 50025, N'La evaluación no existe.', 1;
    IF @id_docente > 0
       AND NOT EXISTS (SELECT 1 FROM dbo.GRUPO WHERE id_grupo = @id_grupo AND id_docente = @id_docente)
        THROW 50011, N'Solo el docente del grupo puede eliminar evaluaciones.', 1;
    UPDATE dbo.EVALUACION SET estado = 0 WHERE id_evaluacion = @id_evaluacion;
    INSERT INTO dbo.AUDITORIA (id_usuario, accion, modulo, tabla_afectada, registro_afectado, descripcion)
    VALUES (@id_usuario, N'Eliminar evaluación', N'Gestión de notas', N'EVALUACION',
            CAST(@id_evaluacion AS NVARCHAR(100)), CONCAT(@asignatura, N' - ', @nombre));
END
GO
