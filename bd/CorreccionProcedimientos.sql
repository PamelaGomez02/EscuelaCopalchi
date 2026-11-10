USE AulaVirtualCopalchi;
GO

-- Lista de alumnos matriculados, incluso sin asistencia previa.
-- @id_docente se conserva por compatibilidad con el repositorio.
-- La autorización de acceso debe seguir aplicándose en la capa de permisos existente.
CREATE OR ALTER PROCEDURE dbo.SP_AS_Clase_Estudiantes
    @id_grupo INT,
    @fecha DATE,
    @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.id_asistencia,
        e.id_estudiante,
        @id_grupo AS id_grupo,
        @fecha AS fecha,
        CONCAT(e.nombre, ' ', e.apellido1, ' ', e.apellido2) AS estudiante,
        a.estado,
        a.observacion
    FROM dbo.ESTUDIANTE AS e
    LEFT JOIN dbo.ASISTENCIA AS a
      ON a.id_estudiante = e.id_estudiante
     AND a.id_grupo = @id_grupo
     AND a.fecha = @fecha
    WHERE e.estado = 1
      AND EXISTS (
          SELECT 1 FROM dbo.MATRICULA AS m
          WHERE m.id_estudiante = e.id_estudiante
            AND m.id_grupo = @id_grupo
      )
    ORDER BY e.apellido1, e.apellido2, e.nombre, e.id_estudiante;
END;
GO

-- El repositorio llama a este nombre, que no existía.
-- Se reutiliza el procedimiento de listado existente.
CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Consultar
    @id_grupo INT,
    @fecha DATE,
    @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.SP_AS_Asistencia_Lista
        @id_grupo = @id_grupo,
        @id_docente = @id_docente,
        @id_estudiante = 0,
        @fecha = @fecha;
END;
GO

-- Historial: reutiliza el listado existente, que admite rango de fechas.
CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Historial
    @id_grupo INT,
    @id_estudiante INT = 0,
    @desde DATE = NULL,
    @hasta DATE = NULL,
    @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    EXEC dbo.SP_AS_Asistencia_Lista
        @id_grupo = @id_grupo,
        @id_docente = @id_docente,
        @id_estudiante = @id_estudiante,
        @desde = @desde,
        @hasta = @hasta;
END;
GO

-- Pruebas
EXEC dbo.SP_AS_Clase_Estudiantes @id_grupo = 1, @fecha = '2026-11-09', @id_docente = 0;
EXEC dbo.SP_AS_Asistencia_Consultar @id_grupo = 1, @fecha = '2026-11-09', @id_docente = 0;
EXEC dbo.SP_AS_Asistencia_Historial @id_grupo = 1, @id_docente = 0;




USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Registrar
    @id_usuario INT,
    @id_docente INT,
    @id_grupo INT,
    @fecha DATE,
    @json NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @id_grupo IS NULL OR @id_grupo <= 0
        THROW 50001, 'Debe seleccionar un grupo.', 1;

    IF @fecha IS NULL
        THROW 50002, 'Debe seleccionar una fecha.', 1;

    IF @id_docente IS NULL OR @id_docente <= 0
        THROW 50003, 'Debe indicar un docente valido.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.GRUPO
        WHERE id_grupo = @id_grupo
    )
        THROW 50004, 'El grupo seleccionado no existe.', 1;

    IF NOT ISJSON(@json) = 1
        THROW 50005, 'Los datos de asistencia no son validos.', 1;

    DECLARE @Filas TABLE
    (
        id_estudiante INT PRIMARY KEY,
        estado NVARCHAR(30),
        observacion NVARCHAR(MAX)
    );

    INSERT INTO @Filas
    (
        id_estudiante,
        estado,
        observacion
    )
    SELECT
        id_estudiante,
        estado,
        observacion
    FROM OPENJSON(@json)
    WITH
    (
        id_estudiante INT '$.id_estudiante',
        estado NVARCHAR(30) '$.estado',
        observacion NVARCHAR(MAX) '$.observacion'
    );

    IF NOT EXISTS (SELECT 1 FROM @Filas)
        THROW 50006, 'No se recibieron estudiantes.', 1;

    IF EXISTS (
        SELECT 1
        FROM @Filas f
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.MATRICULA m
            INNER JOIN dbo.ESTUDIANTE e
                ON e.id_estudiante = m.id_estudiante
            WHERE m.id_estudiante = f.id_estudiante
              AND m.id_grupo = @id_grupo
              AND e.estado = 1
        )
    )
        THROW 50007, 'Uno o mas estudiantes no pertenecen al grupo o estan inactivos.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE a
        SET
            a.estado = f.estado,
            a.observacion = f.observacion
        FROM dbo.ASISTENCIA a
        INNER JOIN @Filas f
            ON f.id_estudiante = a.id_estudiante
        WHERE a.id_grupo = @id_grupo
          AND a.fecha = @fecha;

        INSERT INTO dbo.ASISTENCIA
        (
            id_estudiante,
            id_grupo,
            id_docente,
            fecha,
            estado,
            observacion,
            fecha_registro
        )
        SELECT
            f.id_estudiante,
            @id_grupo,
            @id_docente,
            @fecha,
            f.estado,
            f.observacion,
            SYSDATETIME()
        FROM @Filas f
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.ASISTENCIA a WITH (UPDLOCK, HOLDLOCK)
            WHERE a.id_estudiante = f.id_estudiante
              AND a.id_grupo = @id_grupo
              AND a.fecha = @fecha
        );

        COMMIT TRANSACTION;

        SELECT 'Se ha guardado correctamente' AS mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO



USE AulaVirtualCopalchi;
GO

SELECT
    a.id_asistencia,
    a.id_estudiante,
    e.nombre,
    e.apellido1,
    a.id_grupo,
    a.id_docente,
    a.fecha,
    a.estado,
    a.observacion
FROM dbo.ASISTENCIA a
INNER JOIN dbo.ESTUDIANTE e
    ON a.id_estudiante = e.id_estudiante
ORDER BY a.id_asistencia DESC;


SELECT
    id_grupo,
    fecha,
    estado,
    COUNT(*) AS cantidad
FROM dbo.ASISTENCIA
GROUP BY id_grupo, fecha, estado
ORDER BY fecha DESC;


SELECT
    p.name AS procedimiento,
    prm.name AS parametro,
    TYPE_NAME(prm.user_type_id) AS tipo
FROM sys.procedures p
INNER JOIN sys.parameters prm
    ON p.object_id = prm.object_id
WHERE p.name = 'SP_AS_Asistencia_Consultar'
ORDER BY prm.parameter_id;


USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Consultar
    @id_grupo INT,
    @fecha DATE,
    @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.id_asistencia,
        a.id_estudiante,
        a.id_grupo,
        a.id_docente,
        a.fecha,
        a.estado,
        a.observacion,
        a.fecha_registro,
        e.identificacion,
        e.nombre,
        e.apellido1,
        e.apellido2,
        CONCAT(
            e.nombre, ' ',
            e.apellido1, ' ',
            ISNULL(e.apellido2, '')
        ) AS nombre_completo
    FROM dbo.ASISTENCIA a
    INNER JOIN dbo.ESTUDIANTE e
        ON e.id_estudiante = a.id_estudiante
    WHERE a.id_grupo = @id_grupo
      AND a.fecha = @fecha
      AND (
          @id_docente = 0
          OR a.id_docente = @id_docente
      )
    ORDER BY e.apellido1, e.apellido2, e.nombre;
END;
GO


USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Historial
    @id_grupo INT,
    @id_estudiante INT = 0,
    @desde DATE = NULL,
    @hasta DATE = NULL,
    @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.id_asistencia,
        a.id_estudiante,
        a.id_grupo,
        a.id_docente,
        a.fecha,
        a.estado,
        a.observacion,
        a.fecha_registro,
        e.identificacion,
        e.nombre,
        e.apellido1,
        e.apellido2,
        CONCAT(
            e.nombre, ' ',
            e.apellido1, ' ',
            ISNULL(e.apellido2, '')
        ) AS nombre_completo
    FROM dbo.ASISTENCIA a
    INNER JOIN dbo.ESTUDIANTE e
        ON e.id_estudiante = a.id_estudiante
    WHERE a.id_grupo = @id_grupo
      AND (@id_estudiante IS NULL
           OR @id_estudiante = 0
           OR a.id_estudiante = @id_estudiante)
      AND (@desde IS NULL OR a.fecha >= @desde)
      AND (@hasta IS NULL OR a.fecha <= @hasta)
      AND (@id_docente IS NULL
           OR @id_docente = 0
           OR a.id_docente = @id_docente)
    ORDER BY a.fecha DESC, e.apellido1, e.nombre;
END;
GO


USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Editar
    @id_asistencia INT,
    @estado NVARCHAR(30),
    @observacion NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.ASISTENCIA
        WHERE id_asistencia = @id_asistencia
    )
    BEGIN
        THROW 50001, 'El registro de asistencia no existe.', 1;
    END;

    UPDATE dbo.ASISTENCIA
    SET
        estado = @estado,
        observacion = @observacion
    WHERE id_asistencia = @id_asistencia;

    SELECT
        id_asistencia,
        id_estudiante,
        id_grupo,
        fecha,
        estado,
        observacion
    FROM dbo.ASISTENCIA
    WHERE id_asistencia = @id_asistencia;
END;
GO


USE AulaVirtualCopalchi;
GO

EXEC dbo.SP_AS_Asistencia_Porcentaje
    @id_grupo = 1,
    @id_estudiante = NULL,
    @desde = '2026-11-09',
    @hasta = '2026-11-09';


    
USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Porcentaje
    @id_grupo INT,
    @id_estudiante INT = NULL,
    @desde DATE = NULL,
    @hasta DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.id_estudiante,
        e.identificacion,
        CONCAT(
            e.nombre, ' ',
            e.apellido1, ' ',
            ISNULL(e.apellido2, '')
        ) AS estudiante,

        COUNT(a.id_asistencia) AS total_registros,

        SUM(CASE
            WHEN a.estado IN ('Presente', 'P')
            THEN 1 ELSE 0
        END) AS presentes,

        SUM(CASE
            WHEN a.estado IN ('Ausente', 'A')
            THEN 1 ELSE 0
        END) AS ausentes,

        SUM(CASE
            WHEN a.estado IN ('Tardanza', 'T')
            THEN 1 ELSE 0
        END) AS tardanzas,

        SUM(CASE
            WHEN a.estado IN ('Justificado', 'J')
            THEN 1 ELSE 0
        END) AS justificados,

        CAST(
            CASE
                WHEN COUNT(a.id_asistencia) = 0 THEN NULL
                ELSE
                    100.0 * SUM(
                        CASE
                            WHEN a.estado IN
                                ('Presente', 'P', 'Tardanza', 'T')
                            THEN 1 ELSE 0
                        END
                    ) / COUNT(a.id_asistencia)
            END AS DECIMAL(5,2)
        ) AS porcentaje

    FROM dbo.MATRICULA m

    INNER JOIN dbo.ESTUDIANTE e
        ON e.id_estudiante = m.id_estudiante

    LEFT JOIN dbo.ASISTENCIA a
        ON a.id_estudiante = e.id_estudiante
       AND a.id_grupo = m.id_grupo
       AND (@desde IS NULL OR a.fecha >= @desde)
       AND (@hasta IS NULL OR a.fecha <= @hasta)

    WHERE m.id_grupo = @id_grupo
      AND e.estado = 1
      AND (
          @id_estudiante IS NULL
          OR @id_estudiante = 0
          OR e.id_estudiante = @id_estudiante
      )

    GROUP BY
        e.id_estudiante,
        e.identificacion,
        e.nombre,
        e.apellido1,
        e.apellido2

    ORDER BY
        e.apellido1,
        e.nombre;
END;
GO




CREATE OR ALTER PROCEDURE dbo.SP_GA_Notificacion_MarcarLeida
    @id_notificacion INT,
    @id_usuario INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.GA_Notificacion
    SET leida = 1
    WHERE id_notificacion = @id_notificacion
      AND id_usuario = @id_usuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GA_Notificacion_MarcarTodasLeidas
    @id_usuario INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.GA_Notificacion
    SET leida = 1
    WHERE id_usuario = @id_usuario
      AND leida = 0;
END;
GO

SELECT
    name
FROM sys.procedures
WHERE name IN (
    'SP_GA_Notificacion_MarcarLeida',
    'SP_GA_Notificacion_MarcarTodasLeidas'
);

SELECT
    OBJECT_SCHEMA_NAME(object_id) AS Esquema,
    name AS Procedimiento
FROM sys.procedures
WHERE name LIKE '%Notificacion%';


EXEC sp_helptext 'SP_GA_Notificacion_MarcarTodasLeidas';