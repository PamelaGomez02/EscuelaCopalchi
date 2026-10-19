USE AulaVirtualCopalchi;
GO

/* ============================================================
   MÓDULO ROLES - procedimientos almacenados
   Roles base (no se pueden eliminar): Administrador, Director,
   Docente, Apoyo, Evaluación
   Roles únicos (solo 1 usuario activo): Director, Administrador
   Requiere antes: 7_SP_RolesUnicos.sql (FN_RolEsUnico)
   ============================================================ */

CREATE OR ALTER PROCEDURE SP_Rol_Listar
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id_rol, nombre, descripcion, estado
    FROM ROL
    ORDER BY nombre;
END
GO

CREATE OR ALTER PROCEDURE SP_Rol_ObtenerPorId (@id_rol INT)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id_rol, nombre, descripcion, estado
    FROM ROL
    WHERE id_rol = @id_rol;
END
GO

CREATE OR ALTER PROCEDURE SP_Rol_Insertar
(   @nombre      NVARCHAR(80),
    @descripcion NVARCHAR(255),
    @estado      BIT)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM ROL WHERE LOWER(nombre) = LOWER(LTRIM(RTRIM(@nombre))))
        THROW 50010, N'Ya existe un rol con este nombre.', 1;

    INSERT INTO ROL (nombre, descripcion, estado)
    VALUES (LTRIM(RTRIM(@nombre)), LTRIM(RTRIM(@descripcion)), @estado);
END
GO

CREATE OR ALTER PROCEDURE SP_Rol_Actualizar
(   @id_rol      INT,
    @nombre      NVARCHAR(80),
    @descripcion NVARCHAR(255),
    @estado      BIT)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM ROL
               WHERE LOWER(nombre) = LOWER(LTRIM(RTRIM(@nombre))) AND id_rol <> @id_rol)
        THROW 50011, N'Ya existe otro rol con este nombre.', 1;

    UPDATE ROL
    SET nombre = LTRIM(RTRIM(@nombre)),
        descripcion = LTRIM(RTRIM(@descripcion)),
        estado = @estado
    WHERE id_rol = @id_rol;
END
GO

CREATE OR ALTER PROCEDURE SP_Rol_Eliminar (@id_rol INT)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @nombre NVARCHAR(80) = (SELECT nombre FROM ROL WHERE id_rol = @id_rol);

    IF @nombre IS NULL
        THROW 50012, N'El rol no existe.', 1;

    IF @nombre COLLATE Latin1_General_CI_AI IN
       ('Administrador', 'Director', 'Docente', 'Apoyo', 'Evaluacion')
        THROW 50013, N'Es un rol base del sistema y no se puede eliminar. Puede desactivarlo.', 1;

    IF EXISTS (SELECT 1 FROM USUARIO WHERE id_rol = @id_rol)
        THROW 50014, N'No se puede eliminar el rol porque tiene usuarios asociados (activos o inactivos). Debe cambiarles el rol primero.', 1;

    BEGIN TRY
        BEGIN TRAN;
        DELETE FROM ROL_PERMISO WHERE id_rol = @id_rol;
        DELETE FROM ROL WHERE id_rol = @id_rol;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

/* Usuarios que se pueden asignar (docentes activos) y si ya tienen el rol */
CREATE OR ALTER PROCEDURE SP_Rol_UsuariosAsignables (@id_rol INT)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.id_usuario, u.nombre, u.apellido1, u.apellido2, u.correo,
           CASE WHEN u.id_rol = @id_rol THEN 1 ELSE 0 END AS seleccionado
    FROM USUARIO u
    INNER JOIN DOCENTE d ON d.id_usuario = u.id_usuario
    WHERE u.estado = 1 AND d.estado = 1
    ORDER BY u.nombre, u.apellido1;
END
GO

/* @ids_json: [1,2,3] -> usuarios a los que se asigna el rol */
CREATE OR ALTER PROCEDURE SP_Rol_AsignarUsuarios
(   @id_rol   INT,
    @ids_json NVARCHAR(MAX))
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM ROL WHERE id_rol = @id_rol)
        THROW 50015, N'El rol seleccionado no existe.', 1;

    BEGIN TRY
        BEGIN TRAN;

        UPDATE u
        SET u.id_rol = @id_rol
        FROM USUARIO u
        INNER JOIN DOCENTE d ON d.id_usuario = u.id_usuario
        WHERE u.estado = 1 AND d.estado = 1
          AND u.id_usuario IN (SELECT CAST(value AS INT) FROM OPENJSON(@ids_json));

        -- Rol único (Director / Administrador): máximo 1 usuario activo
        IF dbo.FN_RolEsUnico(@id_rol) = 1
           AND (SELECT COUNT(*) FROM USUARIO WHERE id_rol = @id_rol AND estado = 1) > 1
        BEGIN
            DECLARE @n NVARCHAR(80) = (SELECT nombre FROM ROL WHERE id_rol = @id_rol);
            DECLARE @m NVARCHAR(300) = N'El rol ' + @n + N' solo puede tener un usuario activo. Cambie primero el rol del usuario actual.';
            THROW 50016, @m, 1;
        END

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE SP_Rol_Permisos_Listar (@id_rol INT)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.id_permiso, p.nombre, p.descripcion, p.modulo,
           CASE WHEN rp.id_permiso IS NULL THEN 0 ELSE 1 END AS seleccionado
    FROM PERMISO p
    LEFT JOIN ROL_PERMISO rp ON p.id_permiso = rp.id_permiso AND rp.id_rol = @id_rol
    WHERE p.estado = 1
    ORDER BY p.modulo, p.nombre;
END
GO

/* @ids_json: [1,2,3] -> permisos que quedan asignados al rol */
CREATE OR ALTER PROCEDURE SP_Rol_Permisos_Guardar
(   @id_rol   INT,
    @ids_json NVARCHAR(MAX))
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;
        DELETE FROM ROL_PERMISO WHERE id_rol = @id_rol;

        INSERT INTO ROL_PERMISO (id_rol, id_permiso)
        SELECT DISTINCT @id_rol, CAST(value AS INT)
        FROM OPENJSON(@ids_json);

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO


USE AulaVirtualCopalchi;
GO

ALTER TABLE ESTUDIANTE
ADD observacion_baja NVARCHAR(1000) NULL;
GO

ALTER TABLE ESTUDIANTE
ADD fecha_baja NVARCHAR(1000) NULL;
GO

ALTER TABLE ESTUDIANTE
ADD parentesco NVARCHAR(1000) NULL;
GO

ALTER TABLE ESTUDIANTE
ADD correo_encargado NVARCHAR(1000) NULL;
GO


CREATE TABLE TIPO_ADECUACION
(
id_tipo INT IDENTITY(1,1) PRIMARY KEY,
 
nombre_adecuacion NVARCHAR(100) NOT NULL,
 
descripcion NVARCHAR(500) NULL
);
GO

ALTER TABLE ADECUACION_ACADEMICA
DROP COLUMN tipo;
GO

ALTER TABLE TIPO_ADECUACION
ADD objetivo NVARCHAR(500) NULL;
GO

ALTER TABLE ADECUACION_ACADEMICA
ADD tipo_adecuacion INT NULL;
GO

ALTER TABLE ADECUACION_ACADEMICA
ADD CONSTRAINT FK_ADECUACION_ACADEMICA_TIPO
FOREIGN KEY (tipo_adecuacion)
REFERENCES TIPO_ADECUACION(id_tipo);
GO


INSERT INTO TIPO_ADECUACION
(
    nombre_adecuacion,
    descripcion,
    objetivo
)
VALUES
(
    'Adecuaciones de Acceso',
    'Modificaciones en el entorno físico, recursos materiales o sistemas de comunicación.',
    'Facilitar que el estudiante llegue a la información y participe en las clases.'
),
(
    'Adecuaciones No Significativas',
    'Cambios en la metodología, las actividades o la forma de evaluar, sin alterar los objetivos ni los contenidos básicos del año que se cursa.',
    'Adaptar la enseñanza al ritmo y estilo de aprendizaje del alumno.'
),
(
    'Adecuaciones Significativas',
    'Modificaciones sustanciales que alteran los objetivos, contenidos básicos o criterios de evaluación oficiales.',
    'Responder a necesidades educativas muy específicas que impiden alcanzar el currículo estándar.'
);
GO





CREATE TABLE ALERGIA
(
id_alergia INT IDENTITY(1,1) NOT NULL,
 
id_estudiante INT NOT NULL,
 
alergia NVARCHAR(100) NOT NULL,
 
descripcion NVARCHAR(1000) NULL,
 
fecha_registro DATETIME2 NOT NULL
CONSTRAINT DF_ALERGIA_FECHA_REGISTRO
DEFAULT SYSDATETIME(),
 
CONSTRAINT PK_ALERGIA
PRIMARY KEY (id_alergia),
 
CONSTRAINT FK_ALERGIA_ESTUDIANTE
FOREIGN KEY (id_estudiante)
REFERENCES ESTUDIANTE(id_estudiante)
);
GO

INSERT INTO ALERGIA
(
id_estudiante,
alergia,
descripcion
)
VALUES
(
1,
'Penicilina',
'Reacción alérgica reportada por el encargado.'
),
(
1,
'Maní',
'Evitar el consumo de alimentos que contengan maní o trazas de maní.'
),
(
1,
'Mariscos',
'Presenta reacción alérgica al consumir mariscos y productos derivados.'
);
GO

CREATE TABLE OBSERVACION_MEDICA
(
    id_observacion INT IDENTITY(1,1) NOT NULL,

    id_estudiante INT NOT NULL,

    titulo NVARCHAR(150) NOT NULL,

    observacion NVARCHAR(2000) NOT NULL,

    fecha_registro DATETIME2 NOT NULL
        CONSTRAINT DF_OBSERVACION_FECHA
        DEFAULT SYSDATETIME(),

    CONSTRAINT PK_OBSERVACION_MEDICA
        PRIMARY KEY (id_observacion),

    CONSTRAINT FK_OBSERVACION_ESTUDIANTE
        FOREIGN KEY (id_estudiante)
        REFERENCES ESTUDIANTE(id_estudiante)
);
GO

Select * from ESTUDIANTE 