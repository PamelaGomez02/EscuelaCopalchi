USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE SP_CambiarContrasenaUsuario
(   @id_usuario      INT,
    @contrasena_hash NVARCHAR(255))
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE USUARIO
    SET contrasena_hash = @contrasena_hash
    WHERE id_usuario = @id_usuario;
END
GO

CREATE OR ALTER PROCEDURE SP_ObtenerHashUsuario
(   @id_usuario INT)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT contrasena_hash FROM USUARIO WHERE id_usuario = @id_usuario;
END
GO