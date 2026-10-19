USE AulaVirtualCopalchi;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
/* Script de actualización: ejecutar primero en copia de pruebas.
   Requisitos: SQL Server 2016+ (OPENJSON), tablas ROL, USUARIO,
   ESTUDIANTE, ADECUACION_ACADEMICA, PERMISO, ROL_PERMISO.
   No borra datos existentes. */

/* 1. Roles base: insertar solo los ausentes. No altera roles existentes. */
INSERT INTO dbo.ROL (nombre, descripcion, estado)
SELECT v.nombre, v.descripcion, CONVERT(bit,1)
FROM (VALUES
 (N'Administrador',N'Administra usuarios, configuración y funcionamiento general del sistema.'),
 (N'Director',N'Supervisa y aprueba procesos académicos.'),
 (N'Docente',N'Gestiona planes, asistencia y calificaciones.'),
 (N'Apoyo',N'Registra adecuaciones y seguimiento.'),
 (N'Evaluación',N'Genera reportes y estadísticas.')
) v(nombre,descripcion)
WHERE NOT EXISTS (
 SELECT 1 FROM dbo.ROL r
 WHERE LTRIM(RTRIM(r.nombre)) COLLATE Latin1_General_CI_AI
       = v.nombre COLLATE Latin1_General_CI_AI
);
GO

CREATE OR ALTER FUNCTION dbo.FN_RolEsUnico(@id_rol INT)
RETURNS BIT
AS
BEGIN
 DECLARE @resultado BIT = 0;
 IF EXISTS (SELECT 1 FROM dbo.ROL
            WHERE id_rol=@id_rol
            AND nombre COLLATE Latin1_General_CI_AI IN (N'Administrador',N'Director'))
    SET @resultado=1;
 RETURN @resultado;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Rol_Listar
AS
BEGIN
 SET NOCOUNT ON;
 SELECT id_rol,nombre,descripcion,estado FROM dbo.ROL ORDER BY nombre;
END;
GO
CREATE OR ALTER PROCEDURE dbo.SP_Rol_ObtenerPorId @id_rol INT
AS
BEGIN
 SET NOCOUNT ON;
 SELECT id_rol,nombre,descripcion,estado FROM dbo.ROL WHERE id_rol=@id_rol;
END;
GO
CREATE OR ALTER PROCEDURE dbo.SP_Rol_Insertar
 @nombre NVARCHAR(80), @descripcion NVARCHAR(255), @estado BIT
AS
BEGIN
 SET NOCOUNT ON;
 SET @nombre=NULLIF(LTRIM(RTRIM(@nombre)),N'');
 IF @nombre IS NULL THROW 50009,N'El nombre del rol es obligatorio.',1;
 IF EXISTS (SELECT 1 FROM dbo.ROL WITH (UPDLOCK,HOLDLOCK)
   WHERE nombre COLLATE Latin1_General_CI_AI = @nombre COLLATE Latin1_General_CI_AI)
    THROW 50010,N'Ya existe un rol con este nombre.',1;
 INSERT dbo.ROL(nombre,descripcion,estado) VALUES(@nombre,LTRIM(RTRIM(@descripcion)),@estado);
END;
GO
CREATE OR ALTER PROCEDURE dbo.SP_Rol_Actualizar
 @id_rol INT,@nombre NVARCHAR(80),@descripcion NVARCHAR(255),@estado BIT
AS
BEGIN
 SET NOCOUNT ON;
 SET @nombre=NULLIF(LTRIM(RTRIM(@nombre)),N'');
 IF @nombre IS NULL THROW 50009,N'El nombre del rol es obligatorio.',1;
 DECLARE @anterior NVARCHAR(80);
 SELECT @anterior=nombre FROM dbo.ROL WHERE id_rol=@id_rol;
 IF @anterior IS NULL THROW 50012,N'El rol no existe.',1;
 IF EXISTS (SELECT 1 FROM dbo.ROL WHERE id_rol<>@id_rol
   AND nombre COLLATE Latin1_General_CI_AI = @nombre COLLATE Latin1_General_CI_AI)
    THROW 50011,N'Ya existe otro rol con este nombre.',1;
 IF @anterior COLLATE Latin1_General_CI_AI IN
   (N'Administrador',N'Director',N'Docente',N'Apoyo',N'Evaluacion')
   AND @anterior COLLATE Latin1_General_CI_AI <> @nombre COLLATE Latin1_General_CI_AI
    THROW 50017,N'No se puede cambiar el nombre de un rol base.',1;
 IF @nombre COLLATE Latin1_General_CI_AI IN (N'Administrador',N'Director')
   AND @anterior COLLATE Latin1_General_CI_AI <> @nombre COLLATE Latin1_General_CI_AI
    THROW 50018,N'No se puede convertir otro rol en un rol único.',1;
 UPDATE dbo.ROL SET nombre=@nombre,descripcion=LTRIM(RTRIM(@descripcion)),estado=@estado
 WHERE id_rol=@id_rol;
END;
GO
CREATE OR ALTER PROCEDURE dbo.SP_Rol_Eliminar @id_rol INT
AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @nombre NVARCHAR(80)=(SELECT nombre FROM dbo.ROL WHERE id_rol=@id_rol);
 IF @nombre IS NULL THROW 50012,N'El rol no existe.',1;
 IF @nombre COLLATE Latin1_General_CI_AI IN
   (N'Administrador',N'Director',N'Docente',N'Apoyo',N'Evaluacion')
    THROW 50013,N'No se puede eliminar un rol base.',1;
 IF EXISTS(SELECT 1 FROM dbo.USUARIO WHERE id_rol=@id_rol)
    THROW 50014,N'El rol tiene usuarios asociados.',1;
 SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN;
  DELETE FROM dbo.ROL_PERMISO WHERE id_rol=@id_rol;
  DELETE FROM dbo.ROL WHERE id_rol=@id_rol;
  COMMIT;
 END TRY
 BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  THROW;
 END CATCH
END;
GO
/* Todos los usuarios activos, no solo DOCENTE. */
CREATE OR ALTER PROCEDURE dbo.SP_Rol_UsuariosAsignables @id_rol INT
AS
BEGIN
 SET NOCOUNT ON;
 SELECT u.id_usuario,u.nombre,u.apellido1,u.apellido2,u.correo,
        CASE WHEN u.id_rol=@id_rol THEN 1 ELSE 0 END AS seleccionado
 FROM dbo.USUARIO u WHERE u.estado=1
 ORDER BY u.nombre,u.apellido1,u.apellido2;
END;
GO
/* La lista JSON representa a los usuarios que deben quedar con el rol.
   Los no seleccionados que ya tenían el rol NO se desasignan automáticamente,
   para evitar asignarles un rol inválido. */
CREATE OR ALTER PROCEDURE dbo.SP_Rol_AsignarUsuarios
 @id_rol INT,@ids_json NVARCHAR(MAX)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.ROL WHERE id_rol=@id_rol AND estado=1)
    THROW 50015,N'El rol seleccionado no existe o está inactivo.',1;
 IF ISJSON(@ids_json)<>1 OR LEFT(LTRIM(@ids_json),1)<>N'['
    THROW 50019,N'La lista de usuarios debe ser un arreglo JSON.',1;
 DECLARE @seleccionados TABLE(id_usuario INT PRIMARY KEY);
 IF EXISTS(SELECT 1 FROM OPENJSON(@ids_json)
           WHERE [type]<>2 OR TRY_CONVERT(INT,[value]) IS NULL
              OR TRY_CONVERT(INT,[value])<=0)
    THROW 50020,N'La lista contiene identificadores inválidos.',1;
 INSERT INTO @seleccionados(id_usuario)
 SELECT DISTINCT TRY_CONVERT(INT,[value]) FROM OPENJSON(@ids_json);
 BEGIN TRY
  BEGIN TRAN;
  /* Bloqueo serializable para proteger la comprobación de unicidad durante esta operación. */
  IF EXISTS(SELECT 1 FROM @seleccionados s
    LEFT JOIN dbo.USUARIO u WITH (UPDLOCK,HOLDLOCK) ON u.id_usuario=s.id_usuario
    WHERE u.id_usuario IS NULL OR u.estado<>1)
    THROW 50021,N'Uno o más usuarios no existen o están inactivos.',1;
  IF dbo.FN_RolEsUnico(@id_rol)=1 AND
    ((SELECT COUNT(*) FROM dbo.USUARIO WITH (UPDLOCK,HOLDLOCK)
      WHERE id_rol=@id_rol AND estado=1
      AND id_usuario NOT IN (SELECT id_usuario FROM @seleccionados))
     +(SELECT COUNT(*) FROM @seleccionados))>1
    THROW 50016,N'Administrador y Director admiten como máximo un usuario activo.',1;
  UPDATE u SET id_rol=@id_rol
  FROM dbo.USUARIO u JOIN @seleccionados s ON s.id_usuario=u.id_usuario;
  COMMIT;
 END TRY
 BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  THROW;
 END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.SP_Rol_Permisos_Listar @id_rol INT
AS
BEGIN
 SET NOCOUNT ON;
 SELECT p.id_permiso,p.nombre,p.descripcion,p.modulo,
        CASE WHEN rp.id_permiso IS NULL THEN 0 ELSE 1 END AS seleccionado
 FROM dbo.PERMISO p LEFT JOIN dbo.ROL_PERMISO rp
 ON rp.id_permiso=p.id_permiso AND rp.id_rol=@id_rol
 WHERE p.estado=1 ORDER BY p.modulo,p.nombre;
END;
GO
CREATE OR ALTER PROCEDURE dbo.SP_Rol_Permisos_Guardar
 @id_rol INT,@ids_json NVARCHAR(MAX)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.ROL WHERE id_rol=@id_rol)
    THROW 50022,N'El rol no existe.',1;
 IF ISJSON(@ids_json)<>1 OR LEFT(LTRIM(@ids_json),1)<>N'['
    THROW 50023,N'La lista de permisos debe ser un arreglo JSON.',1;
 IF EXISTS(SELECT 1 FROM OPENJSON(@ids_json)
   WHERE [type]<>2 OR TRY_CONVERT(INT,[value]) IS NULL OR TRY_CONVERT(INT,[value])<=0)
    THROW 50024,N'Hay identificadores de permiso inválidos.',1;
 DECLARE @permisos TABLE(id_permiso INT PRIMARY KEY);
 INSERT INTO @permisos SELECT DISTINCT TRY_CONVERT(INT,[value]) FROM OPENJSON(@ids_json);
 IF EXISTS(SELECT 1 FROM @permisos s LEFT JOIN dbo.PERMISO p
    ON p.id_permiso=s.id_permiso WHERE p.id_permiso IS NULL OR p.estado<>1)
    THROW 50025,N'Hay permisos inexistentes o inactivos.',1;
 BEGIN TRY
  BEGIN TRAN;
  DELETE FROM dbo.ROL_PERMISO WHERE id_rol=@id_rol;
  INSERT dbo.ROL_PERMISO(id_rol,id_permiso)
  SELECT @id_rol,id_permiso FROM @permisos;
  COMMIT;
 END TRY
 BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK;
  THROW;
 END CATCH
END;
GO

/* 2. ESTUDIANTE: añadir columnas solo si faltan. */
IF COL_LENGTH('dbo.ESTUDIANTE','observacion_baja') IS NULL
 ALTER TABLE dbo.ESTUDIANTE ADD observacion_baja NVARCHAR(1000) NULL;
IF COL_LENGTH('dbo.ESTUDIANTE','fecha_baja') IS NULL
 ALTER TABLE dbo.ESTUDIANTE ADD fecha_baja DATETIME2 NULL;
/* Si fecha_baja ya existe como NVARCHAR, no se transforma automáticamente:
   comprobar datos con TRY_CONVERT antes de ALTER COLUMN. */
IF COL_LENGTH('dbo.ESTUDIANTE','parentesco') IS NULL
 ALTER TABLE dbo.ESTUDIANTE ADD parentesco NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.ESTUDIANTE','correo_encargado') IS NULL
 ALTER TABLE dbo.ESTUDIANTE ADD correo_encargado NVARCHAR(254) NULL;
GO

/* 3. Tipos de adecuación. */
IF OBJECT_ID('dbo.TIPO_ADECUACION','U') IS NULL
BEGIN
 CREATE TABLE dbo.TIPO_ADECUACION(
  id_tipo INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  nombre_adecuacion NVARCHAR(100) NOT NULL,
  descripcion NVARCHAR(500) NULL,
  objetivo NVARCHAR(500) NULL
 );
END;
IF COL_LENGTH('dbo.TIPO_ADECUACION','objetivo') IS NULL
 ALTER TABLE dbo.TIPO_ADECUACION ADD objetivo NVARCHAR(500) NULL;
GO
;WITH tipos AS (
 SELECT * FROM (VALUES
  (N'Adecuaciones de Acceso',N'Modificaciones en el entorno físico, recursos materiales o sistemas de comunicación.',N'Facilitar que el estudiante acceda a la información y participe en las clases.'),
  (N'Adecuaciones No Significativas',N'Cambios en la metodología, las actividades o la evaluación sin alterar los objetivos ni los contenidos básicos.',N'Adaptar la enseñanza al ritmo y estilo de aprendizaje del estudiante.'),
  (N'Adecuaciones Significativas',N'Modificaciones sustanciales de objetivos, contenidos o criterios de evaluación.',N'Responder a necesidades educativas específicas.')
 ) v(nombre,descripcion,objetivo)
)
INSERT dbo.TIPO_ADECUACION(nombre_adecuacion,descripcion,objetivo)
SELECT t.nombre,t.descripcion,t.objetivo FROM tipos t
WHERE NOT EXISTS(SELECT 1 FROM dbo.TIPO_ADECUACION a
 WHERE a.nombre_adecuacion COLLATE Latin1_General_CI_AI=t.nombre COLLATE Latin1_General_CI_AI);
GO
IF COL_LENGTH('dbo.ADECUACION_ACADEMICA','tipo_adecuacion') IS NULL
 ALTER TABLE dbo.ADECUACION_ACADEMICA ADD tipo_adecuacion INT NULL;
GO
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_ADECUACION_ACADEMICA_TIPO'
 AND parent_object_id=OBJECT_ID('dbo.ADECUACION_ACADEMICA'))
 ALTER TABLE dbo.ADECUACION_ACADEMICA
 ADD CONSTRAINT FK_ADECUACION_ACADEMICA_TIPO
 FOREIGN KEY(tipo_adecuacion) REFERENCES dbo.TIPO_ADECUACION(id_tipo);
GO
/* No eliminar ADECUACION_ACADEMICA.tipo automáticamente: podría contener datos.
   Migrar primero sus valores a tipo_adecuacion y revisar los resultados. */

/* 4. Datos médicos: crear tablas solo si faltan. */
IF OBJECT_ID('dbo.ALERGIA','U') IS NULL
BEGIN
 CREATE TABLE dbo.ALERGIA(
  id_alergia INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ALERGIA PRIMARY KEY,
  id_estudiante INT NOT NULL,
  alergia NVARCHAR(100) NOT NULL,
  descripcion NVARCHAR(1000) NULL,
  fecha_registro DATETIME2 NOT NULL CONSTRAINT DF_ALERGIA_FECHA_REGISTRO DEFAULT SYSDATETIME(),
  CONSTRAINT FK_ALERGIA_ESTUDIANTE FOREIGN KEY(id_estudiante) REFERENCES dbo.ESTUDIANTE(id_estudiante)
 );
END;
GO
IF OBJECT_ID('dbo.OBSERVACION_MEDICA','U') IS NULL
BEGIN
 CREATE TABLE dbo.OBSERVACION_MEDICA(
  id_observacion INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OBSERVACION_MEDICA PRIMARY KEY,
  id_estudiante INT NOT NULL,
  titulo NVARCHAR(150) NOT NULL,
  observacion NVARCHAR(2000) NOT NULL,
  fecha_registro DATETIME2 NOT NULL CONSTRAINT DF_OBSERVACION_FECHA DEFAULT SYSDATETIME(),
  CONSTRAINT FK_OBSERVACION_ESTUDIANTE FOREIGN KEY(id_estudiante) REFERENCES dbo.ESTUDIANTE(id_estudiante)
 );
END;
GO
/* No insertar alergias ficticias para id_estudiante=1: podría no existir
   o representar una persona real. Registrar solo datos confirmados. */

/* 5. Verificaciones de solo lectura. */
SELECT id_rol,nombre,estado FROM dbo.ROL ORDER BY nombre;
EXEC dbo.SP_Rol_Listar;
SELECT COLUMN_NAME,DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='ESTUDIANTE'
 AND COLUMN_NAME IN ('observacion_baja','fecha_baja','parentesco','correo_encargado');
GO



USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Rol_UsuariosAsignables
    @id_rol INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.id_usuario,
        u.nombre,
        u.apellido1,
        u.apellido2,
        u.correo,
        CASE
            WHEN u.id_rol = @id_rol THEN 1
            ELSE 0
        END AS seleccionado
    FROM dbo.USUARIO u
    WHERE u.estado = 1
    ORDER BY
        u.nombre,
        u.apellido1,
        u.apellido2;
END;
GO




USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Rol_AsignarUsuarios
    @id_rol INT,
    @ids_json NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.ROL
        WHERE id_rol = @id_rol
          AND estado = 1
    )
        THROW 50015, N'El rol no existe o está inactivo.', 1;

    IF ISJSON(@ids_json) <> 1
        THROW 50017, N'La lista de usuarios no es un JSON válido.', 1;

    DECLARE @Usuarios TABLE (
        id_usuario INT PRIMARY KEY
    );

    IF EXISTS (
        SELECT 1
        FROM OPENJSON(@ids_json)
        WHERE [type] <> 2
           OR TRY_CONVERT(INT, [value]) IS NULL
    )
        THROW 50018, N'La lista contiene identificadores inválidos.', 1;

    INSERT INTO @Usuarios (id_usuario)
    SELECT DISTINCT TRY_CONVERT(INT, [value])
    FROM OPENJSON(@ids_json);

    IF EXISTS (
        SELECT 1
        FROM @Usuarios s
        LEFT JOIN dbo.USUARIO u
            ON u.id_usuario = s.id_usuario
        WHERE u.id_usuario IS NULL
           OR u.estado = 0
    )
        THROW 50019, N'Hay usuarios inexistentes o inactivos.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Evitar asignaciones concurrentes incompatibles.
        DECLARE @bloqueo INT;

        EXEC @bloqueo = sp_getapplock
            @Resource = 'AulaVirtualCopalchi_AsignacionRoles',
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @bloqueo < 0
            THROW 50020, N'No se pudo bloquear la asignación de roles.', 1;

        -- Comprobar roles únicos antes de actualizar.
        IF dbo.FN_RolEsUnico(@id_rol) = 1
        BEGIN
            DECLARE @cantidad INT;

            SELECT @cantidad = COUNT(*)
            FROM dbo.USUARIO
            WHERE id_rol = @id_rol
              AND estado = 1
              AND id_usuario NOT IN (
                  SELECT id_usuario FROM @Usuarios
              );

            SELECT @cantidad = @cantidad + COUNT(*)
            FROM @Usuarios;

            IF @cantidad > 1
                THROW 50016,
                    N'Administrador y Director solo pueden tener un usuario activo.',
                    1;
        END;

        UPDATE u
        SET u.id_rol = @id_rol
        FROM dbo.USUARIO u
        INNER JOIN @Usuarios s
            ON s.id_usuario = u.id_usuario
        WHERE u.estado = 1;

        COMMIT TRANSACTION;
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
    u.id_usuario,
    u.nombre,
    u.apellido1,
    u.apellido2,
    u.correo,
    r.nombre AS rol_actual
FROM dbo.USUARIO u
LEFT JOIN dbo.ROL r
    ON r.id_rol = u.id_rol
ORDER BY u.nombre;

EXEC dbo.SP_Rol_UsuariosAsignables @id_rol = 1;



USE AulaVirtualCopalchi;
GO

-- ==========================================
-- MOSTRAR TODOS LOS USUARIOS ACTIVOS
-- ==========================================
CREATE OR ALTER PROCEDURE dbo.SP_Rol_UsuariosAsignables
    @id_rol INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.id_usuario,
        u.nombre,
        u.apellido1,
        u.apellido2,
        u.correo,
        CASE
            WHEN u.id_rol = @id_rol THEN 1
            ELSE 0
        END AS seleccionado
    FROM dbo.USUARIO u
    WHERE u.estado = 1
    ORDER BY
        u.nombre,
        u.apellido1,
        u.apellido2;
END;
GO

-- ==========================================
-- ASIGNAR ROL A USUARIOS
-- ==========================================
CREATE OR ALTER PROCEDURE dbo.SP_Rol_AsignarUsuarios
    @id_rol INT,
    @ids_json NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF ISJSON(@ids_json) <> 1
        THROW 50017,
            N'La lista de usuarios no es JSON válido.', 1;

    IF LEFT(LTRIM(@ids_json), 1) <> '['
        THROW 50018,
            N'Se esperaba un arreglo JSON de usuarios.', 1;

    DECLARE @Seleccionados TABLE
    (
        id_usuario INT PRIMARY KEY
    );

    IF EXISTS (
        SELECT 1
        FROM OPENJSON(@ids_json)
        WHERE [type] <> 2
           OR TRY_CONVERT(INT, [value]) IS NULL
           OR TRY_CONVERT(INT, [value]) <= 0
           OR TRY_CONVERT(DECIMAL(20,4), [value])
                <> TRY_CONVERT(INT, [value])
    )
        THROW 50019,
            N'La lista contiene IDs inválidos.', 1;

    INSERT INTO @Seleccionados (id_usuario)
    SELECT DISTINCT TRY_CONVERT(INT, [value])
    FROM OPENJSON(@ids_json);

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @bloqueo INT;

        EXEC @bloqueo = sys.sp_getapplock
            @Resource = N'AulaVirtualCopalchi_Roles',
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 10000;

        IF @bloqueo < 0
            THROW 50020,
                N'No se pudo bloquear la asignación.', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.ROL WITH (UPDLOCK, HOLDLOCK)
            WHERE id_rol = @id_rol
              AND estado = 1
        )
            THROW 50015,
                N'El rol no existe o está inactivo.', 1;

        IF EXISTS (
            SELECT 1
            FROM @Seleccionados s
            LEFT JOIN dbo.USUARIO u WITH (UPDLOCK, HOLDLOCK)
                ON u.id_usuario = s.id_usuario
            WHERE u.id_usuario IS NULL
               OR u.estado = 0
        )
            THROW 50021,
                N'Hay usuarios inexistentes o inactivos.', 1;

        -- Director y Administrador son roles únicos.
        IF dbo.FN_RolEsUnico(@id_rol) = 1
        BEGIN
            IF (
                SELECT COUNT(*)
                FROM dbo.USUARIO u WITH (UPDLOCK, HOLDLOCK)
                WHERE u.estado = 1
                  AND (
                      u.id_rol = @id_rol
                      OR u.id_usuario IN (
                          SELECT id_usuario
                          FROM @Seleccionados
                      )
                  )
            ) > 1
                THROW 50016,
                    N'Este rol solo admite un usuario activo. Cambie primero el rol del usuario actual.',
                    1;
        END;

        UPDATE u
        SET u.id_rol = @id_rol
        FROM dbo.USUARIO u
        INNER JOIN @Seleccionados s
            ON s.id_usuario = u.id_usuario
        WHERE u.estado = 1
          AND (u.id_rol IS NULL OR u.id_rol <> @id_rol);

        COMMIT TRANSACTION;
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
    c.name AS Columna,
    TYPE_NAME(c.user_type_id) AS TipoDato,
    c.max_length AS Longitud,
    c.is_nullable AS PermiteNulos
FROM sys.columns c
WHERE c.object_id = OBJECT_ID('dbo.USUARIO')
ORDER BY c.column_id;


USE AulaVirtualCopalchi;
GO

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'USUARIO'
ORDER BY ORDINAL_POSITION;

USE AulaVirtualCopalchi;
GO

SELECT id_rol, nombre, estado
FROM dbo.ROL
WHERE nombre = 'Administrador';


USE AulaVirtualCopalchi;
GO

DECLARE @IdRol INT;

SELECT @IdRol = id_rol
FROM dbo.ROL
WHERE nombre = N'Administrador'
  AND estado = 1;

IF @IdRol IS NULL
    THROW 50001,
        N'No existe un rol Administrador activo.', 1;

DECLARE @Correo NVARCHAR(150) =
    N'admin@copalchi.ed.cr';

DECLARE @Hash NVARCHAR(255) =
    N'PEGAR_AQUI_EL_HASH_GENERADO';

IF @Hash NOT LIKE N'PBKDF2-SHA256$%'
    THROW 50002,
        N'Debe generar primero el hash de la contraseña.', 1;

IF EXISTS (
    SELECT 1 FROM dbo.USUARIO
    WHERE correo = @Correo
)
    THROW 50003,
        N'Ya existe un usuario con ese correo.', 1;

IF EXISTS (
    SELECT 1 FROM dbo.USUARIO
    WHERE id_rol = @IdRol
      AND estado = 1
)
    THROW 50004,
        N'Ya existe un Administrador activo.', 1;

INSERT INTO dbo.USUARIO
(
    id_rol,
    identificacion,
    nombre,
    apellido1,
    apellido2,
    correo,
    telefono,
    contrasena_hash,
    estado,
    ultimo_acceso,
    fecha_creacion
)
VALUES
(
    @IdRol,
    N'IDENTIFICACION_REAL',
    N'Administrador',
    N'Sistema',
    NULL,
    @Correo,
    NULL,
    @Hash,
    1,
    NULL,
    SYSDATETIME()
);

SELECT
    id_usuario,
    nombre,
    apellido1,
    correo,
    id_rol,
    estado
FROM dbo.USUARIO
WHERE correo = @Correo;
GO




USE AulaVirtualCopalchi;
GO

DECLARE @IdRol INT;

SELECT @IdRol = id_rol
FROM dbo.ROL
WHERE nombre = N'Administrador'
  AND estado = 1;

IF @IdRol IS NULL
    THROW 50001,
        N'No existe un rol Administrador activo.', 1;

DECLARE @Correo NVARCHAR(150) =
    N'admin@copalchi.ed.cr';

DECLARE @Hash NVARCHAR(255) =
    N'PBKDF2-SHA256$120000$gU6GkSiqid+2DQUXquq0Og==$nD/uvsSFzJvvQRxgRoRqM60/l2t4yF0nFR28FllVhtE=';

IF @Hash NOT LIKE N'PBKDF2-SHA256$%'
    THROW 50002,
        N'Debe generar primero el hash de la contraseña.', 1;

IF EXISTS (
    SELECT 1 FROM dbo.USUARIO
    WHERE correo = @Correo
)
    THROW 50003,
        N'Ya existe un usuario con ese correo.', 1;

IF EXISTS (
    SELECT 1 FROM dbo.USUARIO
    WHERE id_rol = @IdRol
      AND estado = 1
)
    THROW 50004,
        N'Ya existe un Administrador activo.', 1;

INSERT INTO dbo.USUARIO
(
    id_rol,
    identificacion,
    nombre,
    apellido1,
    apellido2,
    correo,
    telefono,
    contrasena_hash,
    estado,
    ultimo_acceso,
    fecha_creacion
)
VALUES
(
    @IdRol,
    N'IDENTIFICACION_REAL',
    N'Administrador',
    N'Sistema',
    NULL,
    @Correo,
    NULL,
    @Hash,
    1,
    NULL,
    SYSDATETIME()
);

SELECT
    id_usuario,
    nombre,
    apellido1,
    correo,
    id_rol,
    estado
FROM dbo.USUARIO
WHERE correo = @Correo;
GO

USE AulaVirtualCopalchi;
GO

SELECT
    COLUMN_NAME AS Columna,
    DATA_TYPE AS TipoDato,
    CHARACTER_MAXIMUM_LENGTH AS Longitud,
    IS_NULLABLE AS PermiteNulos
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'MATERIAL_DIDACTICO'
ORDER BY ORDINAL_POSITION;


USE AulaVirtualCopalchi;
GO

SELECT
    fk.name AS NombreRelacion,
    COL_NAME(fkc.parent_object_id,
             fkc.parent_column_id) AS Columna,
    OBJECT_NAME(fk.referenced_object_id) AS TablaRelacionada,
    COL_NAME(fkc.referenced_object_id,
             fkc.referenced_column_id) AS ColumnaRelacionada
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc
    ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id =
    OBJECT_ID('dbo.MATERIAL_DIDACTICO');


    USE AulaVirtualCopalchi;
GO

SELECT
    TABLE_NAME AS Tabla,
    COLUMN_NAME AS Columna,
    DATA_TYPE AS TipoDato,
    IS_NULLABLE AS PermiteNulos
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN (
    'USUARIO',
    'DOCENTE',
    'ROL',
    'ROL_PERMISO',
    'PERMISO'
)
AND TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME, ORDINAL_POSITION;



USE AulaVirtualCopalchi;
GO

-- Agregar usuario responsable del material
IF COL_LENGTH('dbo.MATERIAL_DIDACTICO',
              'id_usuario_carga') IS NULL
BEGIN
    ALTER TABLE dbo.MATERIAL_DIDACTICO
    ADD id_usuario_carga INT NULL;
END;
GO

-- Crear relación con USUARIO
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_MATERIAL_USUARIO_CARGA'
      AND parent_object_id =
          OBJECT_ID('dbo.MATERIAL_DIDACTICO')
)
BEGIN
    ALTER TABLE dbo.MATERIAL_DIDACTICO
    ADD CONSTRAINT FK_MATERIAL_USUARIO_CARGA
    FOREIGN KEY (id_usuario_carga)
    REFERENCES dbo.USUARIO(id_usuario);
END;
GO


USE AulaVirtualCopalchi;
GO

SELECT
    r.id_rol,
    r.nombre AS Rol,
    p.id_permiso,
    p.nombre AS Permiso,
    p.modulo AS Modulo
FROM dbo.ROL r
INNER JOIN dbo.ROL_PERMISO rp
    ON r.id_rol = rp.id_rol
INNER JOIN dbo.PERMISO p
    ON rp.id_permiso = p.id_permiso
ORDER BY r.nombre, p.modulo, p.nombre;




USE AulaVirtualCopalchi;
GO

BEGIN TRANSACTION;

DECLARE @IdPermiso INT;

SELECT @IdPermiso = id_permiso
FROM dbo.PERMISO
WHERE nombre = 'GESTIONAR_MATERIAL'
  AND estado = 1;

IF @IdPermiso IS NULL
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001,
        'No existe el permiso GESTIONAR_MATERIAL activo.',
        1;
END;

INSERT INTO dbo.ROL_PERMISO (id_rol, id_permiso)
SELECT r.id_rol, @IdPermiso
FROM dbo.ROL r
WHERE r.nombre IN ('Administrador', 'Docente')
  AND r.estado = 1
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.ROL_PERMISO rp
      WHERE rp.id_rol = r.id_rol
        AND rp.id_permiso = @IdPermiso
  );

COMMIT TRANSACTION;
GO

-- Verificar permisos
SELECT
    r.nombre AS Rol,
    p.nombre AS Permiso
FROM dbo.ROL_PERMISO rp
INNER JOIN dbo.ROL r
    ON rp.id_rol = r.id_rol
INNER JOIN dbo.PERMISO p
    ON rp.id_permiso = p.id_permiso
WHERE p.nombre = 'GESTIONAR_MATERIAL';



USE AulaVirtualCopalchi;
GO

SELECT
    COLUMN_NAME AS Columna,
    DATA_TYPE AS TipoDato,
    IS_NULLABLE AS PermiteNulos
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'MATERIAL_DIDACTICO'
  AND COLUMN_NAME IN ('id_docente', 'id_usuario_carga');
 
 
 
 USE AulaVirtualCopalchi;
GO

ALTER TABLE dbo.MATERIAL_DIDACTICO
ALTER COLUMN id_docente INT NULL;
GO

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'MATERIAL_DIDACTICO'
  AND COLUMN_NAME IN ('id_docente', 'id_usuario_carga');



  
USE AulaVirtualCopalchi;
GO

-- Categoría del material
IF COL_LENGTH('dbo.MATERIAL_DIDACTICO', 'categoria') IS NULL
BEGIN
    ALTER TABLE dbo.MATERIAL_DIDACTICO
    ADD categoria NVARCHAR(100) NULL;
END;
GO

-- Grado académico
IF COL_LENGTH('dbo.MATERIAL_DIDACTICO', 'grado') IS NULL
BEGIN
    ALTER TABLE dbo.MATERIAL_DIDACTICO
    ADD grado NVARCHAR(50) NULL;
END;
GO

-- Clasificación general del documento
IF COL_LENGTH('dbo.MATERIAL_DIDACTICO', 'tipo_material') IS NULL
BEGIN
    ALTER TABLE dbo.MATERIAL_DIDACTICO
    ADD tipo_material NVARCHAR(50) NULL;
END;
GO

-- Verificar las columnas
SELECT
    COLUMN_NAME AS Columna,
    DATA_TYPE AS TipoDato,
    IS_NULLABLE AS PermiteNulos
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'MATERIAL_DIDACTICO'
ORDER BY ORDINAL_POSITION;



USE AulaVirtualCopalchi;
GO

BEGIN TRANSACTION;

INSERT INTO dbo.PERMISO
    (nombre, descripcion, modulo, estado)
SELECT
    p.nombre,
    p.descripcion,
    'Material Didáctico',
    1
FROM
(
    VALUES
    ('VER_MATERIAL',
     'Consultar y buscar materiales'),

    ('DESCARGAR_MATERIAL',
     'Descargar archivos del repositorio'),

    ('CARGAR_MATERIAL',
     'Cargar material didáctico'),

    ('CARGAR_DOCUMENTO_INSTITUCIONAL',
     'Cargar documentos institucionales'),

    ('EDITAR_MATERIAL',
     'Editar materiales propios'),

    ('ELIMINAR_MATERIAL',
     'Eliminar materiales propios'),

    ('ADMINISTRAR_TODO_MATERIAL',
     'Editar y eliminar materiales de otros usuarios')
) AS p(nombre, descripcion)
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.PERMISO existente
    WHERE existente.nombre = p.nombre
);

COMMIT TRANSACTION;
GO

SELECT
    id_permiso,
    nombre,
    descripcion,
    modulo,
    estado
FROM dbo.PERMISO
WHERE modulo = 'Material Didáctico'
ORDER BY id_permiso;


USE AulaVirtualCopalchi;
GO

BEGIN TRANSACTION;

DECLARE @IdPermiso INT;

SELECT @IdPermiso = id_permiso
FROM dbo.PERMISO
WHERE nombre = 'GESTIONAR_MATERIAL';

-- Eliminar asignaciones del permiso anterior
DELETE FROM dbo.ROL_PERMISO
WHERE id_permiso = @IdPermiso;

-- Eliminar permiso anterior
DELETE FROM dbo.PERMISO
WHERE id_permiso = @IdPermiso;

COMMIT TRANSACTION;
GO