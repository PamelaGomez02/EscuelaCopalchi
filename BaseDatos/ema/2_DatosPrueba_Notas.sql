/* =====================================================================
   DATOS DE PRUEBA PARA GESTIÓN DE NOTAS (opcional)
   Ejecutar después de 1_SP_GestionNotas.sql y del script de Gestión Académica.
   Crea el grupo "Primero A" (2026) para la docente María González, le
   matricula hasta 5 estudiantes activos que no tengan grupo y le crea
   3 evaluaciones de Matemática del I Trimestre (30 %, 30 % y 40 %).
   Se puede ejecutar varias veces sin duplicar datos.
   ===================================================================== */

USE AulaVirtualCopalchi;
GO

DECLARE @id_docente INT =
    (SELECT d.id_docente
     FROM dbo.DOCENTE d
     JOIN dbo.USUARIO u ON u.id_usuario = d.id_usuario
     WHERE u.correo = N'maria.gonzalez@copalchi.ed.cr');
IF @id_docente IS NULL
    THROW 50030, N'No existe la docente de prueba. Ejecute antes el script de Gestión Académica.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.GRUPO WHERE nombre = N'Primero A' AND anio = 2026)
    INSERT INTO dbo.GRUPO (nombre, nivel, anio, periodo, capacidad, id_aula, id_docente)
    VALUES (N'Primero A', N'Primero', 2026, N'2026', 25,
            (SELECT TOP 1 id_aula FROM dbo.AULA ORDER BY id_aula), @id_docente);
DECLARE @id_grupo INT = (SELECT id_grupo FROM dbo.GRUPO WHERE nombre = N'Primero A' AND anio = 2026);
IF NOT EXISTS (SELECT 1 FROM dbo.MATRICULA WHERE id_grupo = @id_grupo)
    INSERT INTO dbo.MATRICULA (id_estudiante, id_grupo, periodo)
    SELECT TOP (5) e.id_estudiante, @id_grupo, N'2026'
    FROM dbo.ESTUDIANTE e
    WHERE e.estado = 1
      AND NOT EXISTS (SELECT 1 FROM dbo.MATRICULA m
                      WHERE m.id_estudiante = e.id_estudiante AND m.estado = N'Activa')
    ORDER BY e.id_estudiante;
IF NOT EXISTS (SELECT 1 FROM dbo.EVALUACION
               WHERE id_grupo = @id_grupo AND asignatura = N'Matemática' AND trimestre = 1)
    INSERT INTO dbo.EVALUACION (id_grupo, asignatura, trimestre, nombre, porcentaje, fecha, nota_maxima)
    VALUES (@id_grupo, N'Matemática', 1, N'Tarea 1', 30, '2026-02-20', 100),
           (@id_grupo, N'Matemática', 1, N'Trabajo',  30, '2026-03-10', 100),
           (@id_grupo, N'Matemática', 1, N'Examen',   40, '2026-03-25', 100);
SELECT g.id_grupo, g.nombre AS grupo,
       (SELECT COUNT(*) FROM dbo.MATRICULA m WHERE m.id_grupo = g.id_grupo AND m.estado = N'Activa') AS estudiantes,
       (SELECT COUNT(*) FROM dbo.EVALUACION ev WHERE ev.id_grupo = g.id_grupo) AS evaluaciones
FROM dbo.GRUPO g
WHERE g.id_grupo = @id_grupo;
GO


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



