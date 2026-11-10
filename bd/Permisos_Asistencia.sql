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