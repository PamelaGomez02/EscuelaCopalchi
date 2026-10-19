/* Gestión de Notas - apoyo para importar desde Excel */

USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GN_Estudiante_Buscar
    @id_grupo INT = 0   -- 0 = todos los estudiantes; >0 = solo matriculados activos del grupo
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.id_estudiante, e.identificacion
    FROM dbo.ESTUDIANTE e
    WHERE @id_grupo = 0
       OR EXISTS (SELECT 1 FROM dbo.MATRICULA m
                  WHERE m.id_estudiante = e.id_estudiante
                    AND m.id_grupo = @id_grupo AND m.estado = N'Activa');
END
GO


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
