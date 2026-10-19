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



USE AulaVirtualCopalchi;
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

    INSERT INTO USUARIO
    (   id_rol,
        identificacion,
        nombre,
        apellido1,
        apellido2,
        correo,
        telefono,
        contrasena_hash,
        estado)
    VALUES
    (   @id_rol,
        @identificacion,
        @nombre,
        @apellido1,
        @apellido2,
        @correo,
        @telefono,
        @contrasena_hash,
        @estado);
END
GO

CREATE OR ALTER PROCEDURE SP_ListarUsuarios
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.id_usuario,
        u.id_rol,
        r.nombre AS nombre_rol,
        u.identificacion,
        u.nombre,
        u.apellido1,
        u.apellido2,
        u.correo,
        u.telefono,
        u.estado,
        u.ultimo_acceso,
        u.fecha_creacion
    FROM USUARIO u
    INNER JOIN ROL r ON r.id_rol = u.id_rol
    ORDER BY u.nombre, u.apellido1;
END
GO

CREATE OR ALTER PROCEDURE SP_ObtenerUsuarioPorId
(   @id_usuario INT)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.id_usuario,
        u.id_rol,
        r.nombre AS nombre_rol,
        u.identificacion,
        u.nombre,
        u.apellido1,
        u.apellido2,
        u.correo,
        u.telefono,
        u.estado,
        u.ultimo_acceso,
        u.fecha_creacion
    FROM USUARIO u
    INNER JOIN ROL r ON r.id_rol = u.id_rol
    WHERE u.id_usuario = @id_usuario;
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
    -- No se actualiza contrasena_hash ni estado aquí.
    -- El cambio de contraseña y el cambio de estado
    -- son operaciones independientes (SP_CambiarEstadoUsuario
    -- y, en el futuro, SP_CambiarContrasena).
    UPDATE USUARIO
    SET
        id_rol         = @id_rol,
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

    UPDATE USUARIO
    SET estado = @estado
    WHERE id_usuario = @id_usuario;
END
GO

CREATE OR ALTER PROCEDURE SP_ListarRoles_Dropdown
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        id_rol,
        nombre
    FROM ROL
    WHERE estado = 1
    ORDER BY nombre;
END
GO

select * from usuario;