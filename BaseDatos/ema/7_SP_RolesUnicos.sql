USE AulaVirtualCopalchi;
GO

/* ============================================================
   Roles únicos: solo puede existir UN usuario ACTIVO con rol
   Director y UN usuario ACTIVO con rol Administrador.
   Se valida en los procedimientos de Insertar, Actualizar y
   Cambiar estado (reactivar).
   ============================================================ */

CREATE OR ALTER FUNCTION dbo.FN_RolEsUnico (@id_rol INT)
RETURNS BIT
AS
BEGIN
    RETURN (SELECT CASE WHEN EXISTS (
                SELECT 1 FROM ROL
                WHERE id_rol = @id_rol
                  AND nombre COLLATE Latin1_General_CI_AI IN ('Director', 'Administrador'))
            THEN 1 ELSE 0 END);
END
GO

CREATE OR ALTER PROCEDURE SP_InsertarUsuario
(   @id_rol              INT,
    @identificacion      NVARCHAR(30),
    @nombre              NVARCHAR(80),
    @apellido1           NVARCHAR(80),
    @apellido2           NVARCHAR(80),
    @correo              NVARCHAR(150),
    @telefono            NVARCHAR(25),
    @contrasena_hash     NVARCHAR(255),
    @estado              BIT)
AS
BEGIN
    SET NOCOUNT ON;

    IF @estado = 1 AND dbo.FN_RolEsUnico(@id_rol) = 1
       AND EXISTS (SELECT 1 FROM USUARIO WHERE id_rol = @id_rol AND estado = 1)
    BEGIN
        DECLARE @n1 NVARCHAR(50) = (SELECT nombre FROM ROL WHERE id_rol = @id_rol);
        DECLARE @m1 NVARCHAR(200) = N'Ya existe un usuario activo con el rol ' + @n1 + N'. Solo puede haber uno.';
        THROW 50001, @m1, 1;
    END

    INSERT INTO USUARIO
    (   id_rol, identificacion, nombre, apellido1, apellido2,
        correo, telefono, contrasena_hash, estado)
    VALUES
    (   @id_rol, @identificacion, @nombre, @apellido1, @apellido2,
        @correo, @telefono, @contrasena_hash, @estado);
END
GO

CREATE OR ALTER PROCEDURE SP_ActualizarUsuario
(   @id_usuario          INT,
    @id_rol              INT,
    @identificacion      NVARCHAR(30),
    @nombre              NVARCHAR(80),
    @apellido1           NVARCHAR(80),
    @apellido2           NVARCHAR(80),
    @correo              NVARCHAR(150),
    @telefono            NVARCHAR(25))
AS
BEGIN
    SET NOCOUNT ON;

    IF dbo.FN_RolEsUnico(@id_rol) = 1
       AND EXISTS (SELECT 1 FROM USUARIO WHERE id_usuario = @id_usuario AND estado = 1)
       AND EXISTS (SELECT 1 FROM USUARIO
                   WHERE id_rol = @id_rol AND estado = 1 AND id_usuario <> @id_usuario)
    BEGIN
        DECLARE @n2 NVARCHAR(50) = (SELECT nombre FROM ROL WHERE id_rol = @id_rol);
        DECLARE @m2 NVARCHAR(200) = N'Ya existe otro usuario activo con el rol ' + @n2 + N'. Solo puede haber uno.';
        THROW 50001, @m2, 1;
    END

    UPDATE USUARIO
    SET id_rol         = @id_rol,
        identificacion = @identificacion,
        nombre         = @nombre,
        apellido1      = @apellido1,
        apellido2      = @apellido2,
        correo         = @correo,
        telefono       = @telefono
    WHERE id_usuario = @id_usuario;
END
GO

CREATE OR ALTER PROCEDURE SP_CambiarEstadoUsuario
(   @id_usuario INT,
    @estado     BIT)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @rol INT = (SELECT id_rol FROM USUARIO WHERE id_usuario = @id_usuario);

    IF @estado = 1 AND dbo.FN_RolEsUnico(@rol) = 1
       AND EXISTS (SELECT 1 FROM USUARIO
                   WHERE id_rol = @rol AND estado = 1 AND id_usuario <> @id_usuario)
    BEGIN
        DECLARE @n3 NVARCHAR(50) = (SELECT nombre FROM ROL WHERE id_rol = @rol);
        DECLARE @m3 NVARCHAR(200) = N'No se puede activar: ya existe otro usuario activo con el rol ' + @n3 + N'.';
        THROW 50001, @m3, 1;
    END

    UPDATE USUARIO
    SET estado = @estado
    WHERE id_usuario = @id_usuario;
END
GO
