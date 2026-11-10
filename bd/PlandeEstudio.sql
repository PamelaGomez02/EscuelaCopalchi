-- EJECUTE PRIMERO ESTA CONSULTA PARA VER LAS COLUMNAS OBLIGATORIAS DE PERMISO
SELECT c.name, t.name AS tipo, c.is_nullable, c.is_identity, c.default_object_id
FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id
WHERE c.object_id=OBJECT_ID(N'dbo.PERMISO') ORDER BY c.column_id;

-- Permisos necesarios para el controlador:
SELECT nombre FROM dbo.PERMISO WHERE nombre IN
(N'VER_PLANES',N'CREAR_PLANES',N'EDITAR_PLANES',N'REVISAR_PLANES',N'DESCARGAR_PLANES',N'ADMINISTRAR_TODO_PLANES');

-- EJEMPLO (si PERMISO tiene nombre, descripcion, estado y no requiere otras columnas):
-- INSERT INTO dbo.PERMISO (nombre, descripcion, estado)
-- SELECT v.nombre, v.descripcion, 1 FROM (VALUES
-- (N'VER_PLANES',N'Consultar planes'),(N'CREAR_PLANES',N'Subir planes'),
-- (N'EDITAR_PLANES',N'Editar planes propios pendientes'),
-- (N'REVISAR_PLANES',N'Aprobar o denegar planes'),
-- (N'DESCARGAR_PLANES',N'Descargar planes'),
-- (N'ADMINISTRAR_TODO_PLANES',N'Editar planes de otros docentes')
-- ) v(nombre,descripcion) WHERE NOT EXISTS (SELECT 1 FROM dbo.PERMISO p WHERE p.nombre=v.nombre);
-- Después, asigne los permisos desde su módulo Roles.

USE AulaVirtualCopalchi;
GO
CREATE OR ALTER PROCEDURE dbo.SP_PLAN_LISTAR_DOCENTES AS
BEGIN SET NOCOUNT ON;
SELECT d.id_docente, LTRIM(RTRIM(CONCAT(u.nombre,N' ',u.apellido1,N' ',ISNULL(u.apellido2,N'')))) AS nombre_docente
FROM dbo.DOCENTE d INNER JOIN dbo.USUARIO u ON u.id_usuario=d.id_usuario
WHERE d.estado=1 AND u.estado=1 ORDER BY u.nombre,u.apellido1;
END
GO
CREATE OR ALTER PROCEDURE dbo.SP_PLAN_LISTAR_GRUPOS AS
BEGIN SET NOCOUNT ON;
DECLARE @col sysname;
SELECT TOP(1) @col = name FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.GRUPO')
AND name IN (N'nombre_grupo',N'nombre',N'descripcion',N'seccion',N'grado')
ORDER BY CASE name WHEN N'nombre_grupo' THEN 1 WHEN N'nombre' THEN 2 WHEN N'descripcion' THEN 3 WHEN N'seccion' THEN 4 ELSE 5 END;
DECLARE @sql nvarchar(max) = N'SELECT id_grupo, ' +
CASE WHEN @col IS NULL THEN N'''Grupo '' + CONVERT(nvarchar(20), id_grupo)'
ELSE N'COALESCE(CONVERT(nvarchar(150),' + QUOTENAME(@col) + N'), ''Grupo '' + CONVERT(nvarchar(20),id_grupo))' END +
N' AS nombre_grupo FROM dbo.GRUPO ORDER BY id_grupo';
EXEC sys.sp_executesql @sql;
END
GO
CREATE OR ALTER PROCEDURE dbo.SP_GUARDAR_PLAN_ESTUDIO
@id_docente INT,@id_grupo INT,@titulo NVARCHAR(200),@mes TINYINT,@anio INT,@nivel NVARCHAR(50),@asignatura NVARCHAR(100),@estado NVARCHAR(20),@observaciones NVARCHAR(1000),@archivo NVARCHAR(500)
AS BEGIN SET NOCOUNT ON;
INSERT INTO dbo.PLAN_ESTUDIO(id_docente,id_grupo,titulo,mes,anio,nivel,asignatura,estado,observaciones,archivo,fecha_carga)
VALUES(@id_docente,@id_grupo,@titulo,@mes,@anio,@nivel,@asignatura,@estado,@observaciones,@archivo,SYSDATETIME());
END
GO
CREATE OR ALTER PROCEDURE dbo.SP_OBTENER_PLANES_ESTUDIO AS
BEGIN SET NOCOUNT ON;
SELECT p.*, LTRIM(RTRIM(CONCAT(u.nombre,N' ',u.apellido1,N' ',ISNULL(u.apellido2,N'')))) AS nombre_docente
FROM dbo.PLAN_ESTUDIO p INNER JOIN dbo.DOCENTE d ON d.id_docente=p.id_docente INNER JOIN dbo.USUARIO u ON u.id_usuario=d.id_usuario
ORDER BY p.fecha_carga DESC;
END
GO
CREATE OR ALTER PROCEDURE dbo.SP_OBTENER_PLAN_ESTUDIO @id_plan INT AS
BEGIN SET NOCOUNT ON;
SELECT p.*, LTRIM(RTRIM(CONCAT(u.nombre,N' ',u.apellido1,N' ',ISNULL(u.apellido2,N'')))) AS nombre_docente
FROM dbo.PLAN_ESTUDIO p INNER JOIN dbo.DOCENTE d ON d.id_docente=p.id_docente INNER JOIN dbo.USUARIO u ON u.id_usuario=d.id_usuario WHERE p.id_plan=@id_plan;
END
GO
CREATE OR ALTER PROCEDURE dbo.SP_ACTUALIZAR_PLAN_ESTUDIO
@id_plan INT,@id_grupo INT,@titulo NVARCHAR(200),@mes TINYINT,@anio INT,@nivel NVARCHAR(50),@asignatura NVARCHAR(100),@archivo NVARCHAR(500)
AS BEGIN SET NOCOUNT ON;
UPDATE dbo.PLAN_ESTUDIO SET id_grupo=@id_grupo,titulo=@titulo,mes=@mes,anio=@anio,nivel=@nivel,asignatura=@asignatura,archivo=@archivo
WHERE id_plan=@id_plan AND estado=N'Pendiente';
IF @@ROWCOUNT=0 THROW 51000,'No se encontró un plan pendiente para actualizar.',1;
END
GO
CREATE OR ALTER PROCEDURE dbo.SP_REVISAR_PLAN_ESTUDIO
@id_plan INT,@estado NVARCHAR(20),@observaciones NVARCHAR(1000),@id_usuario_revision INT
AS BEGIN SET NOCOUNT ON;
IF @estado NOT IN(N'Aprobado',N'Denegado') THROW 51001,'Estado no válido.',1;
UPDATE dbo.PLAN_ESTUDIO SET estado=@estado,observaciones=@observaciones,fecha_revision=SYSDATETIME(),id_usuario_revision=@id_usuario_revision
WHERE id_plan=@id_plan AND estado=N'Pendiente';
IF @@ROWCOUNT=0 THROW 51002,'El plan no existe o ya fue revisado.',1;
END
GO



USE AulaVirtualCopalchi;
GO

-- Verificar los permisos actuales
SELECT *
FROM dbo.PERMISO
WHERE nombre LIKE '%PLAN%';


SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'PERMISO'
ORDER BY ORDINAL_POSITION;



USE AulaVirtualCopalchi;
GO

DECLARE @Modulo NVARCHAR(150);

-- Utilizar el nombre del módulo que ya existe
SELECT TOP 1
    @Modulo = modulo
FROM dbo.PERMISO
WHERE nombre = 'GESTIONAR_PLANES';

IF @Modulo IS NULL
BEGIN
    THROW 50001,
        'No se encontró GESTIONAR_PLANES. Verifique la tabla PERMISO.',
        1;
END;

-- Crear los permisos faltantes
DECLARE @NuevosPermisos TABLE
(
    nombre NVARCHAR(150),
    descripcion NVARCHAR(250)
);

INSERT INTO @NuevosPermisos
(nombre, descripcion)
VALUES
('VER_PLANES', 'Consultar planes de estudio'),
('CREAR_PLANES', 'Registrar y subir planes de estudio'),
('EDITAR_PLANES', 'Editar planes de estudio propios'),
('REVISAR_PLANES', 'Aprobar o denegar planes de estudio'),
('DESCARGAR_PLANES', 'Descargar archivos de planes de estudio'),
('ADMINISTRAR_TODO_PLANES', 'Administrar planes de otros docentes');

-- Insertar solamente los que no existen
INSERT INTO dbo.PERMISO
(
    nombre,
    descripcion,
    modulo,
    estado
)
SELECT
    np.nombre,
    np.descripcion,
    @Modulo,
    1
FROM @NuevosPermisos np
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.PERMISO p
    WHERE p.nombre = np.nombre
);

-- Comprobar los permisos registrados
SELECT
    id_permiso,
    nombre,
    descripcion,
    modulo,
    estado
FROM dbo.PERMISO
WHERE nombre LIKE '%PLAN%'
ORDER BY nombre;