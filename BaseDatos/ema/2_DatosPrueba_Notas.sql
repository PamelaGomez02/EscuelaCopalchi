/* =====================================================================
   DATOS DE PRUEBA PARA GESTIÓN DE NOTAS (opcional)
   Ejecutar después de 1_SP_GestionNotas.sql y del script de Gestión Académica.
   Crea el grupo "Primero A" (2026) para la docente María González, le
   matricula hasta 5 estudiantes activos que no tengan grupo y le crea
   3 evaluaciones de Matemática del I Trimestre (30 %, 30 % y 40 %).
   Se puede ejecutar varias veces sin duplicar datos.
   ===================================================================== */

USE AulaVirtualCopalchi;
GO

DECLARE @id_docente INT =
    (SELECT d.id_docente
     FROM dbo.DOCENTE d
     JOIN dbo.USUARIO u ON u.id_usuario = d.id_usuario
     WHERE u.correo = N'maria.gonzalez@copalchi.ed.cr');
IF @id_docente IS NULL
    THROW 50030, N'No existe la docente de prueba. Ejecute antes el script de Gestión Académica.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.GRUPO WHERE nombre = N'Primero A' AND anio = 2026)
    INSERT INTO dbo.GRUPO (nombre, nivel, anio, periodo, capacidad, id_aula, id_docente)
    VALUES (N'Primero A', N'Primero', 2026, N'2026', 25,
            (SELECT TOP 1 id_aula FROM dbo.AULA ORDER BY id_aula), @id_docente);
DECLARE @id_grupo INT = (SELECT id_grupo FROM dbo.GRUPO WHERE nombre = N'Primero A' AND anio = 2026);
IF NOT EXISTS (SELECT 1 FROM dbo.MATRICULA WHERE id_grupo = @id_grupo)
    INSERT INTO dbo.MATRICULA (id_estudiante, id_grupo, periodo)
    SELECT TOP (5) e.id_estudiante, @id_grupo, N'2026'
    FROM dbo.ESTUDIANTE e
    WHERE e.estado = 1
      AND NOT EXISTS (SELECT 1 FROM dbo.MATRICULA m
                      WHERE m.id_estudiante = e.id_estudiante AND m.estado = N'Activa')
    ORDER BY e.id_estudiante;
IF NOT EXISTS (SELECT 1 FROM dbo.EVALUACION
               WHERE id_grupo = @id_grupo AND asignatura = N'Matemática' AND trimestre = 1)
    INSERT INTO dbo.EVALUACION (id_grupo, asignatura, trimestre, nombre, porcentaje, fecha, nota_maxima)
    VALUES (@id_grupo, N'Matemática', 1, N'Tarea 1', 30, '2026-02-20', 100),
           (@id_grupo, N'Matemática', 1, N'Trabajo',  30, '2026-03-10', 100),
           (@id_grupo, N'Matemática', 1, N'Examen',   40, '2026-03-25', 100);
SELECT g.id_grupo, g.nombre AS grupo,
       (SELECT COUNT(*) FROM dbo.MATRICULA m WHERE m.id_grupo = g.id_grupo AND m.estado = N'Activa') AS estudiantes,
       (SELECT COUNT(*) FROM dbo.EVALUACION ev WHERE ev.id_grupo = g.id_grupo) AS evaluaciones
FROM dbo.GRUPO g
WHERE g.id_grupo = @id_grupo;
GO
