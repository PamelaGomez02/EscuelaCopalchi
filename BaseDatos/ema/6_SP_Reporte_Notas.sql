/* Gestión de notas - datos para exportar a Excel (grupo completo o un estudiante) */
USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GN_Reporte_Notas
    @id_docente INT = 0,        -- 0 = sin restricción (Director / Evaluación)
    @id_grupo INT,
    @id_estudiante INT = 0      -- 0 = todos los estudiantes del grupo
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.id_estudiante, e.identificacion,
           LTRIM(RTRIM(CONCAT(e.nombre, N' ', e.apellido1, N' ', e.apellido2))) AS estudiante,
           g.nombre AS grupo, g.anio,
           LTRIM(RTRIM(CONCAT(u.nombre, N' ', u.apellido1, N' ', u.apellido2))) AS docente,
           ev.asignatura, ev.trimestre, ev.nombre AS evaluacion, ev.porcentaje, ev.nota_maxima,
           c.nota
    FROM dbo.MATRICULA m
    JOIN dbo.ESTUDIANTE e        ON e.id_estudiante = m.id_estudiante
    JOIN dbo.GRUPO g             ON g.id_grupo = m.id_grupo
    LEFT JOIN dbo.DOCENTE d      ON d.id_docente = g.id_docente
    LEFT JOIN dbo.USUARIO u      ON u.id_usuario = d.id_usuario
    LEFT JOIN dbo.EVALUACION ev  ON ev.id_grupo = m.id_grupo AND ev.estado = 1
    LEFT JOIN dbo.CALIFICACION c ON c.id_evaluacion = ev.id_evaluacion
                                AND c.id_estudiante = e.id_estudiante
    WHERE m.id_grupo = @id_grupo AND m.estado = N'Activa'
      AND (@id_estudiante = 0 OR e.id_estudiante = @id_estudiante)
      AND (@id_docente = 0 OR g.id_docente = @id_docente)
    ORDER BY e.apellido1, e.apellido2, e.nombre, ev.asignatura, ev.trimestre, ev.fecha, ev.id_evaluacion;
END
GO
