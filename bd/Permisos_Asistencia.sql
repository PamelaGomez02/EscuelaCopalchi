USE AulaVirtualCopalchi;
GO
-- Registrar permisos sin duplicar y conservando los existentes.
-- Se requiere que dbo.PERMISO.id_permiso sea IDENTITY.
IF COLUMNPROPERTY(OBJECT_ID('dbo.PERMISO'), 'id_permiso', 'IsIdentity') <> 1
    THROW 50001, 'id_permiso no es IDENTITY. Revise la asignacion de identificadores antes de insertar.', 1;
GO
DECLARE @modulo NVARCHAR(150) = N'Asistencia';
SELECT TOP (1) @modulo = modulo
FROM dbo.PERMISO
WHERE nombre IN (N'REGISTRAR_ASISTENCIA', N'VER_TODA_ASISTENCIA')
ORDER BY CASE WHEN nombre = N'REGISTRAR_ASISTENCIA' THEN 0 ELSE 1 END;

DECLARE @nuevos TABLE (nombre NVARCHAR(150), descripcion NVARCHAR(250));
INSERT INTO @nuevos VALUES
(N'CONSULTAR_ASISTENCIA', N'Consultar asistencia de los grupos autorizados'),
(N'REGISTRAR_ASISTENCIA', N'Registrar asistencia'),
(N'EDITAR_ASISTENCIA', N'Editar registros de asistencia'),
(N'VER_HISTORIAL_ASISTENCIA', N'Consultar historial de asistencia'),
(N'VER_PORCENTAJE_ASISTENCIA', N'Consultar porcentajes de asistencia');

INSERT INTO dbo.PERMISO(nombre, descripcion, modulo, estado)
SELECT n.nombre, n.descripcion, @modulo, 1
FROM @nuevos n
WHERE NOT EXISTS (SELECT 1 FROM dbo.PERMISO p WHERE p.nombre = n.nombre);

SELECT id_permiso, nombre, descripcion, modulo, estado
FROM dbo.PERMISO WHERE modulo = @modulo ORDER BY nombre;


SELECT
    id_permiso,
    nombre,
    descripcion,
    modulo,
    estado
FROM dbo.PERMISO
WHERE nombre LIKE '%ASISTENCIA%'
ORDER BY nombre;


SELECT
    TABLE_NAME,
    COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN (
    'USUARIO',
    'ROL',
    'USUARIO_ROL',
    'ROL_PERMISO'
)
ORDER BY TABLE_NAME, ORDINAL_POSITION;


SELECT
    u.id_usuario,
    u.nombre,
    u.apellido1,
    u.id_rol,
    r.nombre AS nombre_rol,
    u.estado AS usuario_activo,
    r.estado AS rol_activo
FROM dbo.USUARIO u
INNER JOIN dbo.ROL r
    ON u.id_rol = r.id_rol
WHERE u.id_usuario = 3;



USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Grupo_Listar
    @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        g.id_grupo,
        g.nombre AS grupo
    FROM dbo.GRUPO AS g
    WHERE
        @id_docente = 0
        OR EXISTS
        (
            SELECT 1
            FROM dbo.DOCENTE_GRUPO AS dg
            WHERE dg.id_grupo = g.id_grupo
              AND dg.id_docente = @id_docente
        )
    ORDER BY grupo;
END;
GO


USE AulaVirtualCopalchi;
GO

SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN
(
    'GRUPO',
    'DOCENTE_GRUPO',
    'GRUPO_DOCENTE',
    'ASIGNACION_DOCENTE',
    'DOCENTE'
)
ORDER BY TABLE_NAME, ORDINAL_POSITION;

EXEC dbo.SP_AS_Grupo_Listar
    @id_docente = 0;


    SELECT
    DB_NAME() AS BaseDatosActual,
    OBJECT_ID('dbo.SP_AS_Grupo_Listar', 'P') AS IdProcedimiento;


    
USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Grupo_Listar
    @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        g.id_grupo,
        g.nombre AS nombre_grupo,
        g.id_docente,
        g.nivel,
        g.periodo,
        g.anio,
        g.horario
    FROM dbo.GRUPO AS g
    WHERE
        g.estado = 1
        AND (
            @id_docente = 0
            OR g.id_docente = @id_docente
        )
    ORDER BY
        g.nombre;
END;
GO


-- Todos los grupos activos
EXEC dbo.SP_AS_Grupo_Listar
    @id_docente = 0;

-- Grupos asignados al docente 2
EXEC dbo.SP_AS_Grupo_Listar
    @id_docente = 2;


    
USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Grupo_Listar
    @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        g.id_grupo,
        g.nombre AS grupo,
        g.nombre AS nombre_grupo,
        g.id_docente,
        g.nivel,
        g.periodo,
        g.anio,
        g.horario
    FROM dbo.GRUPO AS g
    WHERE
        g.estado = 1
        AND (
            @id_docente = 0
            OR g.id_docente = @id_docente
        )
    ORDER BY
        g.nombre;
END;
GO

-- Comprobar los resultados
EXEC dbo.SP_AS_Grupo_Listar
    @id_docente = 0;
GO



USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Grupo_Listar
    @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        g.id_grupo,
        g.nombre AS grupo,
        g.nombre AS nombre_grupo,
        g.id_docente,
        g.nivel,
        g.periodo,
        g.anio,
        g.horario
    FROM dbo.GRUPO g
    WHERE
        g.estado = 1
        AND (
            @id_docente = 0
            OR g.id_docente = @id_docente
        )
    ORDER BY g.nombre;
END;
GO

EXEC dbo.SP_AS_Grupo_Listar @id_docente = 0;
GO



USE AulaVirtualCopalchi;
GO

SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE COLUMN_NAME IN
(
    'id_grupo',
    'id_estudiante'
)
ORDER BY TABLE_NAME, COLUMN_NAME;


USE AulaVirtualCopalchi;
GO

SELECT
    m.id_grupo,
    m.id_estudiante
FROM dbo.MATRICULA AS m
WHERE m.id_grupo = 1;


USE AulaVirtualCopalchi;
GO

-- Estructura de estudiantes
SELECT
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ESTUDIANTE'
ORDER BY ORDINAL_POSITION;

-- Estructura de matrículas
SELECT
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'MATRICULA'
ORDER BY ORDINAL_POSITION;

-- Estructura de asistencia
SELECT
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ASISTENCIA'
ORDER BY ORDINAL_POSITION;



SELECT
    m.id_grupo,
    m.id_estudiante,
    e.*
FROM dbo.MATRICULA AS m
INNER JOIN dbo.ESTUDIANTE AS e
    ON e.id_estudiante = m.id_estudiante
WHERE m.id_grupo = 1;


SELECT
    name AS Procedimiento,
    CASE
        WHEN OBJECT_ID(
            'dbo.' + name, 'P'
        ) IS NULL
        THEN 'NO EXISTE'
        ELSE 'EXISTE'
    END AS Estado
FROM
(
    VALUES
        ('SP_AS_Asistencia_Lista'),
        ('SP_AS_Asistencia_Porcentaje'),
        ('SP_AS_Grupo_Listar')
) AS P(name);



USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Estudiante_Listar
    @id_grupo INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        e.id_estudiante,
        e.identificacion,
        e.nombre,
        e.apellido1,
        e.apellido2,
        CONCAT(
            e.nombre, ' ',
            e.apellido1, ' ',
            ISNULL(e.apellido2, '')
        ) AS nombre_completo,
        m.id_grupo
    FROM dbo.MATRICULA AS m
    INNER JOIN dbo.ESTUDIANTE AS e
        ON e.id_estudiante = m.id_estudiante
    WHERE m.id_grupo = @id_grupo
      AND e.estado = 1
    ORDER BY
        e.apellido1,
        e.apellido2,
        e.nombre;
END;
GO

EXEC dbo.SP_AS_Estudiante_Listar
    @id_grupo = 1;

    SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'ASISTENCIA'
ORDER BY ORDINAL_POSITION;


USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Lista
    @id_grupo INT,
    @id_estudiante INT = NULL,
    @desde DATE = NULL,
    @hasta DATE = NULL
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
           OR a.id_estudiante = @id_estudiante)
      AND (@desde IS NULL OR a.fecha >= @desde)
      AND (@hasta IS NULL OR a.fecha <= @hasta)
    ORDER BY
        a.fecha DESC,
        e.apellido1,
        e.nombre;
END;
GO


CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Porcentaje
    @id_grupo INT,
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
        ) AS nombre_completo,

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

        CAST(
            CASE
                WHEN COUNT(a.id_asistencia) = 0 THEN 0
                ELSE
                    100.0 * SUM(
                        CASE
                            WHEN a.estado IN
                                ('Presente', 'P', 'Tardanza', 'T')
                            THEN 1 ELSE 0
                        END
                    ) / COUNT(a.id_asistencia)
            END
            AS DECIMAL(5,2)
        ) AS porcentaje_asistencia

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

EXEC dbo.SP_AS_Estudiante_Listar
    @id_grupo = 1;

EXEC dbo.SP_AS_Asistencia_Lista
    @id_grupo = 1;

EXEC dbo.SP_AS_Asistencia_Porcentaje
    @id_grupo = 1;


    USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Lista
    @id_grupo INT,
    @id_estudiante INT = NULL,
    @desde DATE = NULL,
    @hasta DATE = NULL,
    @fecha DATE = NULL
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
    FROM dbo.ASISTENCIA AS a
    INNER JOIN dbo.ESTUDIANTE AS e
        ON e.id_estudiante = a.id_estudiante
    WHERE a.id_grupo = @id_grupo
      AND (
          @id_estudiante IS NULL
          OR a.id_estudiante = @id_estudiante
      )
      AND (
          @fecha IS NULL
          OR a.fecha = @fecha
      )
      AND (
          @desde IS NULL
          OR a.fecha >= @desde
      )
      AND (
          @hasta IS NULL
          OR a.fecha <= @hasta
      )
    ORDER BY
        a.fecha DESC,
        e.apellido1,
        e.nombre;
END;
GO


USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Lista
    @id_grupo INT,
    @id_docente INT = 0,
    @id_estudiante INT = NULL,
    @fecha DATE = NULL,
    @desde DATE = NULL,
    @hasta DATE = NULL
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
      AND (@id_docente = 0 OR a.id_docente = @id_docente)
      AND (@id_estudiante IS NULL OR a.id_estudiante = @id_estudiante)
      AND (@fecha IS NULL OR a.fecha = @fecha)
      AND (@desde IS NULL OR a.fecha >= @desde)
      AND (@hasta IS NULL OR a.fecha <= @hasta)
    ORDER BY
        a.fecha DESC,
        e.apellido1,
        e.nombre;
END;
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
        ) AS nombre_completo,

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

        CAST(
            CASE
                WHEN COUNT(a.id_asistencia) = 0 THEN 0
                ELSE
                    100.0 *
                    SUM(CASE
                        WHEN a.estado IN
                            ('Presente', 'P', 'Tardanza', 'T')
                        THEN 1 ELSE 0
                    END)
                    / COUNT(a.id_asistencia)
            END
            AS DECIMAL(5,2)
        ) AS porcentaje_asistencia

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

-- Historial
EXEC dbo.SP_AS_Asistencia_Lista
    @id_grupo = 1,
    @id_docente = 0;

-- Registro por fecha
EXEC dbo.SP_AS_Asistencia_Lista
    @id_grupo = 1,
    @fecha = '2026-11-09',
    @id_docente = 0;

-- Porcentajes
EXEC dbo.SP_AS_Asistencia_Porcentaje
    @id_grupo = 1,
    @id_estudiante = NULL;


    USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_AS_Asistencia_Historial
    @id_grupo INT,
    @id_docente INT = 0,
    @id_estudiante INT = NULL,
    @desde DATE = NULL,
    @hasta DATE = NULL
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

    WHERE
        a.id_grupo = @id_grupo

        AND (
            @id_docente = 0
            OR a.id_docente = @id_docente
        )

        AND (
            @id_estudiante IS NULL
            OR a.id_estudiante = @id_estudiante
        )

        AND (
            @desde IS NULL
            OR a.fecha >= @desde
        )

        AND (
            @hasta IS NULL
            OR a.fecha <= @hasta
        )

    ORDER BY
        a.fecha DESC,
        e.apellido1,
        e.nombre;
END;
GO


USE AulaVirtualCopalchi;
GO

EXEC dbo.SP_AS_Asistencia_Lista
    @id_grupo = 1,
    @id_docente = 0;


    USE AulaVirtualCopalchi;
GO

SELECT DISTINCT
    e.id_estudiante,
    e.identificacion,
    e.nombre,
    e.apellido1,
    e.apellido2,
    CONCAT(
        e.nombre, ' ',
        e.apellido1, ' ',
        e.apellido2
    ) AS nombre_completo,
    m.id_grupo
FROM dbo.MATRICULA m
INNER JOIN dbo.ESTUDIANTE e
    ON e.id_estudiante = m.id_estudiante
WHERE m.id_grupo = 1
  AND e.estado = 1
ORDER BY e.apellido1, e.apellido2, e.nombre;


USE AulaVirtualCopalchi;
GO

SELECT
    p.name AS Procedimiento,
    par.name AS Parametro,
    TYPE_NAME(par.user_type_id) AS TipoDato
FROM sys.procedures p
INNER JOIN sys.parameters par
    ON p.object_id = par.object_id
WHERE p.name IN (
    'SP_AS_Asistencia_Consultar',
    'SP_AS_Asistencia_Porcentaje',
    'SP_AS_Asistencia_Lista'
)
ORDER BY p.name, par.parameter_id;



USE AulaVirtualCopalchi;
GO

SELECT DISTINCT
    e.id_estudiante,
    e.identificacion,
    e.nombre,
    e.apellido1,
    e.apellido2,
    CONCAT(
        e.nombre, ' ',
        e.apellido1, ' ',
        e.apellido2
    ) AS nombre_completo,
    m.id_grupo
FROM dbo.MATRICULA m
INNER JOIN dbo.ESTUDIANTE e
    ON e.id_estudiante = m.id_estudiante
WHERE m.id_grupo = 1
  AND e.estado = 1
ORDER BY e.apellido1, e.apellido2, e.nombre;


