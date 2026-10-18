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
