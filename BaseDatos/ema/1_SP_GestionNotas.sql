/* =====================================================================
   MÓDULO: GESTIÓN DE NOTAS
   Base de datos: AulaVirtualCopalchi
   ---------------------------------------------------------------------
   Se ejecuta DESPUÉS de los scripts de Gestión Académica (usa PERMISO,
   ROL_PERMISO, AUDITORIA, GRUPO, MATRICULA, ESTUDIANTE, DOCENTE).
   Se puede ejecutar completo las veces que se necesite.

   Reglas que se validan aquí (así valen igual al registrar a mano o al
   importar desde Excel):
     - Solo el docente del grupo registra o modifica notas y evaluaciones.
     - La nota va de 0 a la nota máxima de la evaluación.
     - El estudiante debe tener matrícula activa en el grupo de la evaluación.
     - Los porcentajes de una asignatura en un período no pasan de 100 %.
   Convención: @id_docente = 0 significa "sin restricción de docente".
   ===================================================================== */

USE AulaVirtualCopalchi;
GO

/* =====================================================================
   1. CAMBIOS DE ESTRUCTURA
   ===================================================================== */

-- Las vistas filtran por asignatura y trimestre, y la BD no los tenía.
-- Las evaluaciones que ya existan quedan como asignatura "General" y trimestre 1.
IF COL_LENGTH(N'dbo.EVALUACION', N'asignatura') IS NULL
    ALTER TABLE dbo.EVALUACION ADD asignatura NVARCHAR(100) NOT NULL
        CONSTRAINT DF_EVALUACION_ASIGNATURA DEFAULT N'General';
GO

IF COL_LENGTH(N'dbo.EVALUACION', N'trimestre') IS NULL
    ALTER TABLE dbo.EVALUACION ADD trimestre TINYINT NOT NULL
        CONSTRAINT DF_EVALUACION_TRIMESTRE DEFAULT 1
        CONSTRAINT CK_EVALUACION_TRIMESTRE CHECK (trimestre BETWEEN 1 AND 3);
GO

-- Notas de estudiantes que vienen de otra escuela (no pertenecen a ningún grupo
-- ni evaluación de esta escuela, por eso no afectan promedios de los grupos).
IF OBJECT_ID(N'dbo.CALIFICACION_EXTERNA', N'U') IS NULL
    CREATE TABLE dbo.CALIFICACION_EXTERNA
    (   id_calificacion_externa INT IDENTITY(1,1) CONSTRAINT PK_CALIFICACION_EXTERNA PRIMARY KEY,
        id_estudiante           INT NOT NULL
            CONSTRAINT FK_CALEXT_ESTUDIANTE REFERENCES dbo.ESTUDIANTE(id_estudiante),
        escuela_procedencia     NVARCHAR(150) NOT NULL,
        asignatura              NVARCHAR(100) NOT NULL,
        periodo                 NVARCHAR(50)  NOT NULL,
        anio                    INT NOT NULL CONSTRAINT CK_CALEXT_ANIO CHECK (anio >= 2000),
        nota                    DECIMAL(6,2) NOT NULL CONSTRAINT CK_CALEXT_NOTA CHECK (nota BETWEEN 0 AND 100),
        id_usuario_registro     INT NULL
            CONSTRAINT FK_CALEXT_USUARIO REFERENCES dbo.USUARIO(id_usuario),
        fecha_registro          DATETIME2(0) NOT NULL CONSTRAINT DF_CALEXT_FECHA DEFAULT SYSDATETIME(),
        CONSTRAINT UQ_CALEXT UNIQUE (id_estudiante, escuela_procedencia, asignatura, anio, periodo));
GO

/* =====================================================================
   2. PERMISOS DEL MÓDULO (se ajustan luego en Roles → Permisos)
   ===================================================================== */

INSERT INTO dbo.PERMISO (nombre, descripcion, modulo)
SELECT v.nombre, v.descripcion, v.modulo
FROM (VALUES
        (N'REGISTRAR_NOTAS', N'Registrar, importar, modificar y consultar notas de sus grupos (docentes)', N'Gestión de Notas'),
        (N'VER_TODAS_NOTAS', N'Consultar las notas de todos los grupos (solo lectura)',                    N'Gestión de Notas')
     ) v (nombre, descripcion, modulo)
WHERE NOT EXISTS (SELECT 1 FROM dbo.PERMISO p WHERE p.nombre = v.nombre);

INSERT INTO dbo.ROL_PERMISO (id_rol, id_permiso)
SELECT r.id_rol, p.id_permiso
FROM (VALUES
        (N'Docente',       N'REGISTRAR_NOTAS'),
        (N'Director',      N'VER_TODAS_NOTAS'),
        (N'Administrador', N'VER_TODAS_NOTAS')
     ) v (rol, permiso)
JOIN dbo.ROL r     ON r.nombre = v.rol
JOIN dbo.PERMISO p ON p.nombre = v.permiso
WHERE NOT EXISTS (SELECT 1 FROM dbo.ROL_PERMISO x WHERE x.id_rol = r.id_rol AND x.id_permiso = p.id_permiso);
GO

/* =====================================================================
   3. GRUPOS Y EVALUACIONES
   ===================================================================== */

-- Pantalla Index. @id_docente = 0 -> todos los grupos (director). @anio = 0 -> todos.
-- Pendientes = total_estudiantes * total_evaluaciones - notas_registradas (se calcula en C#).
CREATE OR ALTER PROCEDURE dbo.SP_GN_Grupo_Resumen
    @id_docente INT = 0, @anio INT = 0
AS
    SELECT g.id_grupo, g.nombre AS grupo, g.nivel, g.anio,
           LTRIM(RTRIM(CONCAT(u.nombre, N' ', u.apellido1, N' ', u.apellido2))) AS docente,
           (SELECT COUNT(*) FROM dbo.MATRICULA m
             WHERE m.id_grupo = g.id_grupo AND m.estado = N'Activa') AS total_estudiantes,
           (SELECT COUNT(*) FROM dbo.EVALUACION ev
             WHERE ev.id_grupo = g.id_grupo AND ev.estado = 1) AS total_evaluaciones,
           (SELECT COUNT(*)
              FROM dbo.CALIFICACION c
              JOIN dbo.EVALUACION ev ON ev.id_evaluacion = c.id_evaluacion AND ev.estado = 1
              JOIN dbo.MATRICULA m   ON m.id_estudiante = c.id_estudiante
                                    AND m.id_grupo = ev.id_grupo AND m.estado = N'Activa'
             WHERE ev.id_grupo = g.id_grupo) AS notas_registradas
    FROM dbo.GRUPO g
    LEFT JOIN dbo.DOCENTE d ON d.id_docente = g.id_docente
    LEFT JOIN dbo.USUARIO u ON u.id_usuario = d.id_usuario
    WHERE g.estado = 1
      AND (@id_docente = 0 OR g.id_docente = @id_docente)
      AND (@anio = 0 OR g.anio = @anio)
    ORDER BY g.anio DESC, g.nombre;
GO

-- Asignaturas que ya tienen evaluaciones en el grupo (para el filtro).
CREATE OR ALTER PROCEDURE dbo.SP_GN_Asignatura_Listar
    @id_grupo INT
AS
    SELECT DISTINCT asignatura
    FROM dbo.EVALUACION
    WHERE id_grupo = @id_grupo AND estado = 1
    ORDER BY asignatura;
GO

-- @asignatura = '' y @trimestre = 0 -> sin filtro.
CREATE OR ALTER PROCEDURE dbo.SP_GN_Evaluacion_Listar
    @id_grupo INT, @asignatura NVARCHAR(100) = N'', @trimestre TINYINT = 0
AS
    SELECT ev.id_evaluacion, ev.id_grupo, ev.asignatura, ev.trimestre, ev.nombre,
           ev.porcentaje, ev.fecha, ev.nota_maxima,
           SUM(ev.porcentaje) OVER (PARTITION BY ev.asignatura, ev.trimestre) AS porcentaje_total
    FROM dbo.EVALUACION ev
    WHERE ev.id_grupo = @id_grupo AND ev.estado = 1
      AND (ISNULL(@asignatura, N'') = N'' OR ev.asignatura = @asignatura)
      AND (@trimestre = 0 OR ev.trimestre = @trimestre)
    ORDER BY ev.trimestre, ev.asignatura, ev.fecha, ev.id_evaluacion;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GN_Evaluacion_Insertar
    @id_grupo INT, @asignatura NVARCHAR(100), @trimestre TINYINT, @nombre NVARCHAR(150),
    @porcentaje DECIMAL(5,2), @fecha DATE = NULL, @nota_maxima DECIMAL(6,2) = 100,
    @id_usuario INT, @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.GRUPO WHERE id_grupo = @id_grupo AND estado = 1)
        THROW 50020, N'El grupo no existe o está inactivo.', 1;
    IF @id_docente > 0
       AND NOT EXISTS (SELECT 1 FROM dbo.GRUPO WHERE id_grupo = @id_grupo AND id_docente = @id_docente)
        THROW 50011, N'Solo el docente del grupo puede crear evaluaciones.', 1;
    IF @porcentaje <= 0 OR @porcentaje > 100
        THROW 50023, N'El porcentaje debe ser mayor que 0 y no puede superar 100.', 1;
    IF @nota_maxima <= 0
        THROW 50024, N'La nota máxima debe ser mayor que 0.', 1;
    IF EXISTS (SELECT 1 FROM dbo.EVALUACION
               WHERE id_grupo = @id_grupo AND asignatura = @asignatura AND trimestre = @trimestre
                 AND estado = 1 AND nombre = @nombre)
        THROW 50021, N'Ya existe una evaluación con ese nombre en la asignatura y el período.', 1;
    DECLARE @suma_actual DECIMAL(6,2) =
        (SELECT ISNULL(SUM(porcentaje), 0) FROM dbo.EVALUACION
         WHERE id_grupo = @id_grupo AND asignatura = @asignatura AND trimestre = @trimestre
           AND estado = 1);
    IF @suma_actual + @porcentaje > 100
        THROW 50022, N'La suma de los porcentajes de la asignatura en este período no puede superar 100 %.', 1;
    INSERT INTO dbo.EVALUACION (id_grupo, asignatura, trimestre, nombre, porcentaje, fecha, nota_maxima)
    VALUES (@id_grupo, @asignatura, @trimestre, @nombre, @porcentaje, @fecha, @nota_maxima);
    DECLARE @id_nueva INT = SCOPE_IDENTITY();
    INSERT INTO dbo.AUDITORIA (id_usuario, accion, modulo, tabla_afectada, registro_afectado, descripcion)
    VALUES (@id_usuario, N'Crear evaluación', N'Gestión de notas', N'EVALUACION',
            CAST(@id_nueva AS NVARCHAR(100)),
            CONCAT(@asignatura, N' - ', @nombre, N' (', @porcentaje, N' %)'));
END
GO

/* =====================================================================
   4. CALIFICACIONES
   ===================================================================== */

-- Pantalla Registrar: una fila por estudiante y evaluación (nota NULL si aún no tiene).
-- Si el grupo no tiene evaluaciones para ese filtro, los estudiantes salen con evaluación NULL.
CREATE OR ALTER PROCEDURE dbo.SP_GN_Calificacion_Matriz
    @id_grupo INT, @asignatura NVARCHAR(100), @trimestre TINYINT
AS
    SELECT e.id_estudiante, e.identificacion,
           LTRIM(RTRIM(CONCAT(e.nombre, N' ', e.apellido1, N' ', e.apellido2))) AS nombre_completo,
           ev.id_evaluacion, ev.nombre AS evaluacion, ev.porcentaje, ev.nota_maxima,
           c.id_calificacion, c.nota
    FROM dbo.MATRICULA m
    JOIN dbo.ESTUDIANTE e ON e.id_estudiante = m.id_estudiante
    LEFT JOIN dbo.EVALUACION ev ON ev.id_grupo = m.id_grupo AND ev.estado = 1
                               AND ev.asignatura = @asignatura AND ev.trimestre = @trimestre
    LEFT JOIN dbo.CALIFICACION c ON c.id_evaluacion = ev.id_evaluacion
                                AND c.id_estudiante = e.id_estudiante
    WHERE m.id_grupo = @id_grupo AND m.estado = N'Activa'
    ORDER BY e.apellido1, e.apellido2, e.nombre, ev.fecha, ev.id_evaluacion;
GO

-- Pantalla Editar: una nota con los datos para mostrarla y para saber de qué docente es.
CREATE OR ALTER PROCEDURE dbo.SP_GN_Calificacion_Obtener
    @id_calificacion INT
AS
    SELECT c.id_calificacion, c.id_evaluacion, c.id_estudiante, c.nota, c.observacion, c.fecha_registro,
           ev.nombre AS evaluacion, ev.asignatura, ev.trimestre, ev.porcentaje, ev.nota_maxima,
           g.id_grupo, g.nombre AS grupo, g.id_docente,
           LTRIM(RTRIM(CONCAT(e.nombre, N' ', e.apellido1, N' ', e.apellido2))) AS estudiante
    FROM dbo.CALIFICACION c
    JOIN dbo.EVALUACION ev ON ev.id_evaluacion = c.id_evaluacion
    JOIN dbo.GRUPO g       ON g.id_grupo = ev.id_grupo
    JOIN dbo.ESTUDIANTE e  ON e.id_estudiante = c.id_estudiante
    WHERE c.id_calificacion = @id_calificacion;
GO

-- Registrar, editar e importar usan este mismo procedimiento. Recibe un arreglo JSON:
--   [{"id_evaluacion":1,"id_estudiante":5,"nota":85.5,"observacion":"..."}, ...]
-- Todo o nada: si una fila no cumple las reglas no se guarda ninguna. Las notas vacías
-- (null) se ignoran. Registra en AUDITORIA solo los cambios reales (nota anterior -> nueva).
CREATE OR ALTER PROCEDURE dbo.SP_GN_Calificacion_GuardarLote
    @id_usuario INT, @id_docente INT = 0, @json NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @lote TABLE
    (   id_evaluacion INT NOT NULL,
        id_estudiante INT NOT NULL,
        nota          DECIMAL(6,2) NULL,
        observacion   NVARCHAR(500) NULL,
        PRIMARY KEY (id_evaluacion, id_estudiante));
    INSERT INTO @lote (id_evaluacion, id_estudiante, nota, observacion)
    SELECT id_evaluacion, id_estudiante, nota, observacion
    FROM OPENJSON(@json)
    WITH (id_evaluacion INT           '$.id_evaluacion',
          id_estudiante INT           '$.id_estudiante',
          nota          DECIMAL(6,2)  '$.nota',
          observacion   NVARCHAR(500) '$.observacion');
    DELETE FROM @lote WHERE nota IS NULL;
    IF NOT EXISTS (SELECT 1 FROM @lote)
        THROW 50010, N'No hay notas para guardar.', 1;
    IF EXISTS (SELECT 1 FROM @lote l
               LEFT JOIN dbo.EVALUACION ev ON ev.id_evaluacion = l.id_evaluacion
               WHERE ev.id_evaluacion IS NULL OR ev.estado = 0)
        THROW 50014, N'Hay evaluaciones que no existen o están inactivas.', 1;
    IF @id_docente > 0
       AND EXISTS (SELECT 1 FROM @lote l
                   JOIN dbo.EVALUACION ev ON ev.id_evaluacion = l.id_evaluacion
                   JOIN dbo.GRUPO g       ON g.id_grupo = ev.id_grupo
                   WHERE ISNULL(g.id_docente, 0) <> @id_docente)
        THROW 50011, N'Solo el docente del grupo puede registrar o modificar sus calificaciones.', 1;
    IF EXISTS (SELECT 1 FROM @lote l
               JOIN dbo.EVALUACION ev ON ev.id_evaluacion = l.id_evaluacion
               WHERE NOT EXISTS (SELECT 1 FROM dbo.MATRICULA m
                                 WHERE m.id_estudiante = l.id_estudiante
                                   AND m.id_grupo = ev.id_grupo AND m.estado = N'Activa'))
        THROW 50012, N'Hay estudiantes que no pertenecen al grupo de la evaluación.', 1;
    IF EXISTS (SELECT 1 FROM @lote l
               JOIN dbo.EVALUACION ev ON ev.id_evaluacion = l.id_evaluacion
               WHERE l.nota < 0 OR l.nota > ev.nota_maxima)
        THROW 50013, N'Hay notas fuera del rango permitido (de 0 a la nota máxima de la evaluación).', 1;
    BEGIN TRANSACTION;

    -- Bitácora: solo notas nuevas o que cambian
    INSERT INTO dbo.AUDITORIA (id_usuario, accion, modulo, tabla_afectada, registro_afectado, descripcion)
    SELECT @id_usuario,
           CASE WHEN c.id_calificacion IS NULL THEN N'Registrar nota' ELSE N'Modificar nota' END,
           N'Gestión de notas', N'CALIFICACION',
           CONCAT(l.id_evaluacion, N'/', l.id_estudiante),
           CASE WHEN c.id_calificacion IS NULL
                THEN CONCAT(N'Nota ', l.nota)
                ELSE CONCAT(N'Nota ', c.nota, N' -> ', l.nota) END
    FROM @lote l
    LEFT JOIN dbo.CALIFICACION c ON c.id_evaluacion = l.id_evaluacion AND c.id_estudiante = l.id_estudiante
    WHERE c.id_calificacion IS NULL OR c.nota <> l.nota;
    UPDATE c
    SET c.nota = l.nota,
        c.observacion = ISNULL(l.observacion, c.observacion),
        c.fecha_registro = SYSDATETIME()
    FROM dbo.CALIFICACION c
    JOIN @lote l ON l.id_evaluacion = c.id_evaluacion AND l.id_estudiante = c.id_estudiante
    WHERE c.nota <> l.nota OR (l.observacion IS NOT NULL AND ISNULL(c.observacion, N'') <> l.observacion);
    INSERT INTO dbo.CALIFICACION (id_evaluacion, id_estudiante, nota, observacion)
    SELECT l.id_evaluacion, l.id_estudiante, l.nota, l.observacion
    FROM @lote l
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CALIFICACION c
                      WHERE c.id_evaluacion = l.id_evaluacion AND c.id_estudiante = l.id_estudiante);
    COMMIT TRANSACTION;
END
GO

/* =====================================================================
   5. NOTAS DE ESCUELA ANTERIOR (importación desde Excel)
   ===================================================================== */

-- Arreglo JSON:
--   [{"id_estudiante":5,"escuela_procedencia":"Escuela X","asignatura":"Matemática",
--     "periodo":"I Trimestre","anio":2025,"nota":88}, ...]
-- Si ya existe la misma nota (estudiante, escuela, asignatura, año y período) se actualiza.
CREATE OR ALTER PROCEDURE dbo.SP_GN_CalificacionExterna_GuardarLote
    @id_usuario INT, @id_docente INT = 0, @json NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @lote TABLE
    (   id_estudiante       INT NOT NULL,
        escuela_procedencia NVARCHAR(150) NOT NULL,
        asignatura          NVARCHAR(100) NOT NULL,
        periodo             NVARCHAR(50)  NOT NULL,
        anio                INT NOT NULL,
        nota                DECIMAL(6,2) NOT NULL,
        PRIMARY KEY (id_estudiante, escuela_procedencia, asignatura, anio, periodo));
    INSERT INTO @lote (id_estudiante, escuela_procedencia, asignatura, periodo, anio, nota)
    SELECT id_estudiante, escuela_procedencia, asignatura, periodo, anio, nota
    FROM OPENJSON(@json)
    WITH (id_estudiante       INT           '$.id_estudiante',
          escuela_procedencia NVARCHAR(150) '$.escuela_procedencia',
          asignatura          NVARCHAR(100) '$.asignatura',
          periodo             NVARCHAR(50)  '$.periodo',
          anio                INT           '$.anio',
          nota                DECIMAL(6,2)  '$.nota');
    IF NOT EXISTS (SELECT 1 FROM @lote)
        THROW 50010, N'No hay notas para guardar.', 1;
    IF EXISTS (SELECT 1 FROM @lote l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.ESTUDIANTE e
                                 WHERE e.id_estudiante = l.id_estudiante AND e.estado = 1))
        THROW 50015, N'Hay estudiantes que no existen o están inactivos.', 1;
    IF @id_docente > 0
       AND EXISTS (SELECT 1 FROM @lote l
                   WHERE NOT EXISTS (SELECT 1 FROM dbo.MATRICULA m
                                     JOIN dbo.GRUPO g ON g.id_grupo = m.id_grupo
                                     WHERE m.id_estudiante = l.id_estudiante
                                       AND m.estado = N'Activa' AND g.id_docente = @id_docente))
        THROW 50011, N'Solo puede registrar notas anteriores de estudiantes de sus grupos.', 1;
    IF EXISTS (SELECT 1 FROM @lote WHERE nota < 0 OR nota > 100)
        THROW 50013, N'Hay notas fuera del rango permitido (de 0 a 100).', 1;
    BEGIN TRANSACTION;
    UPDATE x
    SET x.nota = l.nota, x.id_usuario_registro = @id_usuario, x.fecha_registro = SYSDATETIME()
    FROM dbo.CALIFICACION_EXTERNA x
    JOIN @lote l ON l.id_estudiante = x.id_estudiante AND l.escuela_procedencia = x.escuela_procedencia
                AND l.asignatura = x.asignatura AND l.anio = x.anio AND l.periodo = x.periodo;
    INSERT INTO dbo.CALIFICACION_EXTERNA
        (id_estudiante, escuela_procedencia, asignatura, periodo, anio, nota, id_usuario_registro)
    SELECT l.id_estudiante, l.escuela_procedencia, l.asignatura, l.periodo, l.anio, l.nota, @id_usuario
    FROM @lote l
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CALIFICACION_EXTERNA x
                      WHERE x.id_estudiante = l.id_estudiante AND x.escuela_procedencia = l.escuela_procedencia
                        AND x.asignatura = l.asignatura AND x.anio = l.anio AND x.periodo = l.periodo);
    INSERT INTO dbo.AUDITORIA (id_usuario, accion, modulo, tabla_afectada, descripcion)
    VALUES (@id_usuario, N'Importar notas de escuela anterior', N'Gestión de notas',
            N'CALIFICACION_EXTERNA',
            CONCAT(N'Se guardaron ', (SELECT COUNT(*) FROM @lote), N' notas de escuelas anteriores.'));
    COMMIT TRANSACTION;
END
GO

/* =====================================================================
   6. HISTORIAL
   ===================================================================== */

-- Notas de esta escuela y, para los mismos estudiantes, las de escuelas anteriores.
-- @id_docente = 0 -> todos los grupos (director). Los demás filtros: 0 / '' = sin filtro.
-- Las notas de escuela anterior no tienen trimestre propio: con @trimestre > 0 no se incluyen.
CREATE OR ALTER PROCEDURE dbo.SP_GN_Calificacion_Historial
    @id_docente INT = 0, @id_grupo INT = 0, @id_estudiante INT = 0,
    @asignatura NVARCHAR(100) = N'', @trimestre TINYINT = 0
AS
    SELECT TOP (1000) h.*
    FROM (
        SELECT N'Escuela' AS origen, c.id_calificacion, c.id_estudiante,
               LTRIM(RTRIM(CONCAT(e.nombre, N' ', e.apellido1, N' ', e.apellido2))) AS estudiante,
               g.nombre AS grupo, ev.asignatura,
               CASE ev.trimestre WHEN 1 THEN N'I Trimestre' WHEN 2 THEN N'II Trimestre'
                                 ELSE N'III Trimestre' END AS periodo,
               g.anio, ev.nombre AS evaluacion, c.nota, ev.nota_maxima,
               CAST(NULL AS NVARCHAR(150)) AS escuela_procedencia,
               c.fecha_registro AS fecha, g.id_docente
        FROM dbo.CALIFICACION c
        JOIN dbo.EVALUACION ev ON ev.id_evaluacion = c.id_evaluacion AND ev.estado = 1
        JOIN dbo.GRUPO g       ON g.id_grupo = ev.id_grupo
        JOIN dbo.ESTUDIANTE e  ON e.id_estudiante = c.id_estudiante
        WHERE (@id_docente = 0 OR g.id_docente = @id_docente)
          AND (@id_grupo = 0 OR g.id_grupo = @id_grupo)
          AND (@id_estudiante = 0 OR c.id_estudiante = @id_estudiante)
          AND (ISNULL(@asignatura, N'') = N'' OR ev.asignatura = @asignatura)
          AND (@trimestre = 0 OR ev.trimestre = @trimestre)
        UNION ALL
        SELECT N'Escuela anterior', CAST(NULL AS INT), x.id_estudiante,
               LTRIM(RTRIM(CONCAT(e.nombre, N' ', e.apellido1, N' ', e.apellido2))),
               CAST(NULL AS NVARCHAR(100)), x.asignatura, x.periodo, x.anio,
               N'Nota de escuela anterior', x.nota, CAST(100 AS DECIMAL(6,2)),
               x.escuela_procedencia, x.fecha_registro, CAST(NULL AS INT)
        FROM dbo.CALIFICACION_EXTERNA x
        JOIN dbo.ESTUDIANTE e ON e.id_estudiante = x.id_estudiante
        WHERE @trimestre = 0
          AND (@id_estudiante = 0 OR x.id_estudiante = @id_estudiante)
          AND (ISNULL(@asignatura, N'') = N'' OR x.asignatura = @asignatura)
          AND ((@id_docente = 0 AND @id_grupo = 0)
               OR EXISTS (SELECT 1 FROM dbo.MATRICULA m
                          JOIN dbo.GRUPO g ON g.id_grupo = m.id_grupo
                          WHERE m.id_estudiante = x.id_estudiante AND m.estado = N'Activa'
                            AND (@id_docente = 0 OR g.id_docente = @id_docente)
                            AND (@id_grupo = 0 OR g.id_grupo = @id_grupo)))
    ) h
    ORDER BY h.fecha DESC, h.estudiante;
GO

/* Gestión de notas - acceso propio del módulo (Director, Docente, Evaluación) */
CREATE OR ALTER PROCEDURE dbo.SP_GN_Acceso_Usuarios
    @id_usuario INT = 0      -- 0 = todos los usuarios habilitados; >0 = solo ese usuario
AS
BEGIN
    SET NOCOUNT ON;

    SELECT u.id_usuario,
           LTRIM(RTRIM(CONCAT(u.nombre, N' ', u.apellido1, N' ', u.apellido2))) AS nombre_completo,
           u.identificacion,
           r.nombre AS rol,
           ISNULL(d.id_docente, 0) AS id_docente
    FROM dbo.USUARIO u
    JOIN dbo.ROL r           ON r.id_rol = u.id_rol
    LEFT JOIN dbo.DOCENTE d  ON d.id_usuario = u.id_usuario
    WHERE u.estado = 1
      AND r.nombre IN (N'Director', N'Docente', N'Evaluación', N'Evaluacion')
      AND (@id_usuario = 0 OR u.id_usuario = @id_usuario)
    ORDER BY r.nombre, u.nombre, u.apellido1;
END
GO

/* =====================================================================
   VERIFICACIÓN: permisos de Gestión de Notas por rol
   ===================================================================== */

SELECT r.nombre AS rol, p.nombre AS permiso
FROM dbo.ROL_PERMISO rp
JOIN dbo.ROL r     ON r.id_rol = rp.id_rol
JOIN dbo.PERMISO p ON p.id_permiso = rp.id_permiso
WHERE p.nombre IN (N'REGISTRAR_NOTAS', N'VER_TODAS_NOTAS')
ORDER BY r.nombre, p.nombre;
GO
