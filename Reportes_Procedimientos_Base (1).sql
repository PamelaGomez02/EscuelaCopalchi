USE [AulaVirtualCopalchi];
GO
/* No altera tablas ni procedimientos de otros módulos. */
CREATE OR ALTER PROCEDURE dbo.SP_RP_Rendimiento
 @id_docente INT = 0, @id_grupo INT = 0, @busqueda NVARCHAR(200) = N''
AS
BEGIN
 SET NOCOUNT ON;
 ;WITH Inscritos AS (
  SELECT DISTINCT g.id_grupo, g.nombre AS grupo, e.id_estudiante, e.identificacion,
   LTRIM(RTRIM(CONCAT(e.nombre,N' ',e.apellido1,N' ',COALESCE(e.apellido2,N'')))) AS estudiante
  FROM dbo.MATRICULA m
  JOIN dbo.GRUPO g ON g.id_grupo=m.id_grupo AND g.estado=1
  JOIN dbo.ESTUDIANTE e ON e.id_estudiante=m.id_estudiante AND e.estado=1
  WHERE m.estado=N'Activa' AND (@id_docente=0 OR g.id_docente=@id_docente)
   AND (@id_grupo=0 OR g.id_grupo=@id_grupo)
   AND (@busqueda=N'' OR e.identificacion LIKE N'%'+@busqueda+N'%' OR
     CONCAT(e.nombre,N' ',e.apellido1,N' ',COALESCE(e.apellido2,N'')) LIKE N'%'+@busqueda+N'%')
 ), Notas AS (
  SELECT i.id_grupo,i.id_estudiante,
   CAST(AVG(CASE WHEN ev.nota_maxima>0 THEN 100.0*c.nota/ev.nota_maxima END) AS DECIMAL(9,2)) AS promedio,
   COUNT(CASE WHEN c.id_calificacion IS NOT NULL AND ev.nota_maxima>0 THEN 1 END) AS periodos
  FROM Inscritos i
  LEFT JOIN dbo.EVALUACION ev ON ev.id_grupo=i.id_grupo AND ev.estado=1
  LEFT JOIN dbo.CALIFICACION c ON c.id_evaluacion=ev.id_evaluacion AND c.id_estudiante=i.id_estudiante
  GROUP BY i.id_grupo,i.id_estudiante
 ), Asis AS (
  SELECT i.id_grupo,i.id_estudiante,
   COUNT(a.id_asistencia) AS clases,
   SUM(CASE WHEN a.estado=N'Presente' THEN 1 ELSE 0 END) AS presentes,
   SUM(CASE WHEN a.estado=N'Tardanza' THEN 1 ELSE 0 END) AS tardanzas,
   SUM(CASE WHEN a.estado IN (N'Justificada',N'Justificado') THEN 1 ELSE 0 END) AS justificados,
   SUM(CASE WHEN a.estado IN (N'Ausente',N'Ausencia') THEN 1 ELSE 0 END) AS ausentes
  FROM Inscritos i
  LEFT JOIN dbo.ASISTENCIA a ON a.id_grupo=i.id_grupo AND a.id_estudiante=i.id_estudiante
  GROUP BY i.id_grupo,i.id_estudiante
 )
 SELECT i.id_grupo,i.grupo,i.id_estudiante,i.identificacion,i.estudiante,
  n.promedio,n.periodos,a.clases,a.presentes,a.tardanzas,a.justificados,a.ausentes,
  CAST(CASE WHEN a.clases>0 THEN 100.0*(a.clases-a.ausentes)/a.clases END AS DECIMAL(9,2)) AS pct_asistencia,
  CAST(CASE WHEN n.promedio<70 THEN 1 ELSE 0 END AS INT) AS bajo_rendimiento,
  CAST(CASE WHEN a.clases>0 AND 100.0*(a.clases-a.ausentes)/a.clases<80 THEN 1 ELSE 0 END AS INT) AS baja_asistencia
 FROM Inscritos i JOIN Notas n ON n.id_grupo=i.id_grupo AND n.id_estudiante=i.id_estudiante
 JOIN Asis a ON a.id_grupo=i.id_grupo AND a.id_estudiante=i.id_estudiante
 ORDER BY i.grupo,i.estudiante;
END;
GO
CREATE OR ALTER PROCEDURE dbo.SP_RP_RendimientoAsignatura
 @id_docente INT = 0, @id_grupo INT = 0, @busqueda NVARCHAR(200) = N''
AS
BEGIN
 SET NOCOUNT ON;
 ;WITH Inscritos AS (
  SELECT DISTINCT g.id_grupo,e.id_estudiante
  FROM dbo.MATRICULA m
  JOIN dbo.GRUPO g ON g.id_grupo=m.id_grupo AND g.estado=1
  JOIN dbo.ESTUDIANTE e ON e.id_estudiante=m.id_estudiante AND e.estado=1
  WHERE m.estado=N'Activa' AND (@id_docente=0 OR g.id_docente=@id_docente)
   AND (@id_grupo=0 OR g.id_grupo=@id_grupo)
   AND (@busqueda=N'' OR e.identificacion LIKE N'%'+@busqueda+N'%' OR
      CONCAT(e.nombre,N' ',e.apellido1,N' ',COALESCE(e.apellido2,N'')) LIKE N'%'+@busqueda+N'%')
 )
 SELECT COALESCE(NULLIF(LTRIM(RTRIM(ev.asignatura)),N''),N'Sin asignatura') AS asignatura,
  CAST(AVG(100.0*c.nota/NULLIF(ev.nota_maxima,0)) AS DECIMAL(9,2)) AS promedio,
  COUNT(DISTINCT c.id_estudiante) AS estudiantes
 FROM Inscritos i
 JOIN dbo.EVALUACION ev ON ev.id_grupo=i.id_grupo AND ev.estado=1 AND ev.nota_maxima>0
 JOIN dbo.CALIFICACION c ON c.id_evaluacion=ev.id_evaluacion AND c.id_estudiante=i.id_estudiante
 GROUP BY COALESCE(NULLIF(LTRIM(RTRIM(ev.asignatura)),N''),N'Sin asignatura')
 ORDER BY asignatura;
END;
GO
-- Prueba: EXEC dbo.SP_RP_Rendimiento 0,0,N'';
-- Prueba: EXEC dbo.SP_RP_RendimientoAsignatura 0,0,N'';




    USE AulaVirtualCopalchi;
GO

SELECT
    OBJECT_NAME(p.object_id) AS Procedimiento,
    p.name AS Parametro,
    TYPE_NAME(p.user_type_id) AS TipoDato,
    p.max_length AS Longitud
FROM sys.parameters p
WHERE p.object_id IN (
    OBJECT_ID('dbo.SP_RP_Rendimiento'),
    OBJECT_ID('dbo.SP_RP_RendimientoAsignatura')
)
ORDER BY Procedimiento, p.parameter_id;