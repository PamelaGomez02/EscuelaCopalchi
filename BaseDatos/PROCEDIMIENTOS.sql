USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE SP_InsertarEstudiante
(
    @identificacion      NVARCHAR(30),
    @nombre              NVARCHAR(80),
    @apellido1           NVARCHAR(80),
    @apellido2           NVARCHAR(80),
    @fecha_nacimiento    DATE,
    @direccion           NVARCHAR(300),

    @nombre_encargado    NVARCHAR(150),
    @telefono_encargado  NVARCHAR(25),
    @correo_encargado    NVARCHAR(150),
    @parentesco          NVARCHAR(50),

    @estado              BIT
)
AS
BEGIN

    SET NOCOUNT ON;

    INSERT INTO ESTUDIANTE
    (
        identificacion,
        nombre,
        apellido1,
        apellido2,
        fecha_nacimiento,
        direccion,
        nombre_encargado,
        telefono_encargado,
        correo_encargado,
        parentesco,
        estado
    )
    VALUES
    (
        @identificacion,
        @nombre,
        @apellido1,
        @apellido2,
        @fecha_nacimiento,
        @direccion,
        @nombre_encargado,
        @telefono_encargado,
        @correo_encargado,
        @parentesco,
        @estado
    );

END
GO


EXEC SP_InsertarEstudiante
    @identificacion='123',
    @nombre='Juan',
    @apellido1='Perez',
    @apellido2='Lopez',
    @fecha_nacimiento='2015-01-01',
    @direccion='Casa',
    @nombre_encargado='Maria',
    @parentesco='Madre',
    @telefono_encargado='88888888',
    @correo_encargado='maria@test.com',
    @estado=1


CREATE OR ALTER PROCEDURE SP_ListarEstudiantes
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        id_estudiante,
        identificacion,
        nombre,
        apellido1,
        apellido2,
        nombre_encargado,
        telefono_encargado,
        correo_encargado,
        parentesco,
        estado
    FROM ESTUDIANTE
    ORDER BY nombre, apellido1;
END
GO

CREATE OR ALTER PROCEDURE SP_ObtenerEstudiantePorId
(
    @id_estudiante INT
)
AS
BEGIN

    SET NOCOUNT ON;

    SELECT
        id_estudiante,
        identificacion,
        nombre,
        apellido1,
        apellido2,
        fecha_nacimiento,
        direccion,
        nombre_encargado,
        telefono_encargado,
        correo_encargado,
        parentesco,
        estado,
        fecha_registro
    FROM ESTUDIANTE
    WHERE id_estudiante = @id_estudiante;

END
GO

CREATE OR ALTER PROCEDURE SP_ActualizarEstudiante
(
    @id_estudiante INT,
    @identificacion NVARCHAR(50),
    @nombre NVARCHAR(100),
    @apellido1 NVARCHAR(100),
    @apellido2 NVARCHAR(100),
    @fecha_nacimiento DATE,
    @direccion NVARCHAR(250),
    @nombre_encargado NVARCHAR(150),
    @parentesco NVARCHAR(50),
    @telefono_encargado NVARCHAR(50),
    @correo_encargado NVARCHAR(150),
    @estado BIT
)
AS
BEGIN

    SET NOCOUNT ON;

    UPDATE ESTUDIANTE
    SET
        identificacion = @identificacion,
        nombre = @nombre,
        apellido1 = @apellido1,
        apellido2 = @apellido2,
        fecha_nacimiento = @fecha_nacimiento,
        direccion = @direccion,
        nombre_encargado = @nombre_encargado,
        parentesco = @parentesco,
        telefono_encargado = @telefono_encargado,
        correo_encargado = @correo_encargado,
        estado = @estado
    WHERE id_estudiante = @id_estudiante;

END
GO


CREATE OR ALTER PROCEDURE SP_DarBajaEstudiante
(
    @id_estudiante INT,
    @observacion_baja NVARCHAR(1000)
)
AS
BEGIN

    UPDATE ESTUDIANTE
    SET
        estado = 0,
        fecha_baja = GETDATE(),
        observacion_baja = @observacion_baja
    WHERE id_estudiante = @id_estudiante;

END
GO


CREATE OR ALTER PROCEDURE SP_GUARDAR_ADECUACION_ACADEMICA
(
    @id_estudiante INT,
    @id_usuario_registro INT,
    @tipo_adecuacion INT,
    @descripcion NVARCHAR(MAX),
    @medidas NVARCHAR(MAX),
    @fecha_inicio DATE,
    @fecha_fin DATE = NULL,
    @estado NVARCHAR(20)
)
AS
BEGIN

    INSERT INTO ADECUACION_ACADEMICA
    (
        id_estudiante,
        id_usuario_registro,
        tipo_adecuacion,
        descripcion,
        medidas,
        fecha_inicio,
        fecha_fin,
        estado,
        fecha_registro
    )
    VALUES
    (
        @id_estudiante,
        @id_usuario_registro,
        @tipo_adecuacion,
        @descripcion,
        @medidas,
        @fecha_inicio,
        @fecha_fin,
        @estado,
        GETDATE()
    )

END
GO

CREATE OR ALTER PROCEDURE SP_OBTENER_ADECUACIONES_ESTUDIANTE
(
    @id_estudiante INT
)
AS
BEGIN

    SELECT
        A.id_adecuacion,
        A.id_estudiante,
        A.tipo_adecuacion,
        T.nombre_adecuacion,
        A.descripcion,
        A.medidas,
        A.fecha_inicio,
        A.fecha_fin,
        A.estado,
        A.fecha_registro
    FROM ADECUACION_ACADEMICA A
        INNER JOIN TIPO_ADECUACION T
            ON A.tipo_adecuacion = T.id_tipo
    WHERE A.id_estudiante = @id_estudiante
    ORDER BY A.fecha_registro DESC;

END
GO



CREATE OR ALTER PROCEDURE SP_ACTUALIZAR_ADECUACION_ACADEMICA
(
    @id_adecuacion INT,
    @tipo_adecuacion INT,
    @descripcion NVARCHAR(1500),
    @medidas NVARCHAR(1500),
    @fecha_inicio DATE,
    @fecha_fin DATE = NULL,
    @estado NVARCHAR(20)
)
AS
BEGIN

    UPDATE ADECUACION_ACADEMICA
    SET
        tipo_adecuacion = @tipo_adecuacion,
        descripcion = @descripcion,
        medidas = @medidas,
        fecha_inicio = @fecha_inicio,
        fecha_fin = @fecha_fin,
        estado = @estado
    WHERE id_adecuacion = @id_adecuacion

END
GO

CREATE OR ALTER PROCEDURE SP_OBTENER_ALERGIAS
(
    @id_estudiante INT
)
AS
BEGIN

    SELECT
        id_alergia,
        id_estudiante,
        alergia,
        descripcion,
        fecha_registro
    FROM dbo.ALERGIA
    WHERE id_estudiante = @id_estudiante
    ORDER BY fecha_registro DESC;

END
GO

SELECT *
FROM ALERGIA


CREATE OR ALTER PROCEDURE SP_GUARDAR_ALERGIA
(
    @id_estudiante INT,
    @alergia NVARCHAR(100),
    @descripcion NVARCHAR(1000)
)
AS
BEGIN

    INSERT INTO ALERGIA
    (
        id_estudiante,
        alergia,
        descripcion
    )
    VALUES
    (
        @id_estudiante,
        @alergia,
        @descripcion
    )

END
GO


CREATE OR ALTER PROCEDURE SP_ACTUALIZAR_ALERGIA
(
    @id_alergia INT,
    @alergia NVARCHAR(100),
    @descripcion NVARCHAR(1000)
)
AS
BEGIN

    UPDATE ALERGIA
    SET
        alergia = @alergia,
        descripcion = @descripcion
    WHERE id_alergia = @id_alergia;

END
GO

CREATE OR ALTER PROCEDURE SP_ELIMINAR_ALERGIA
(
    @id_alergia INT
)
AS
BEGIN

    DELETE FROM ALERGIA
    WHERE id_alergia = @id_alergia;

END
GO




CREATE OR ALTER PROCEDURE SP_GUARDAR_OBSERVACION
(
    @id_estudiante INT,
    @titulo NVARCHAR(150),
    @observacion NVARCHAR(2000)
)
AS
BEGIN

    INSERT INTO OBSERVACION_MEDICA
    (
        id_estudiante,
        titulo,
        observacion
    )
    VALUES
    (
        @id_estudiante,
        @titulo,
        @observacion
    );

END
GO

CREATE OR ALTER PROCEDURE SP_OBTENER_OBSERVACIONES
(
    @id_estudiante INT
)
AS
BEGIN

    SELECT
        id_observacion,
        id_estudiante,
        titulo,
        observacion,
        fecha_registro
    FROM OBSERVACION_MEDICA
    WHERE id_estudiante = @id_estudiante
    ORDER BY fecha_registro DESC;

END
GO

CREATE OR ALTER PROCEDURE SP_ACTUALIZAR_OBSERVACION
(
    @id_observacion INT,
    @titulo NVARCHAR(150),
    @observacion NVARCHAR(2000)
)
AS
BEGIN

    UPDATE OBSERVACION_MEDICA
    SET
        titulo = @titulo,
        observacion = @observacion
    WHERE id_observacion = @id_observacion

END
GO

CREATE OR ALTER PROCEDURE SP_ELIMINAR_OBSERVACION
(
    @id_observacion INT
)
AS
BEGIN

    DELETE FROM OBSERVACION_MEDICA
    WHERE id_observacion = @id_observacion

END
GO



CREATE OR ALTER PROCEDURE SP_OBTENER_HISTORIAL_BAJAS
AS
BEGIN

    SELECT
        id_estudiante,
        identificacion,
        nombre,
        apellido1,
        apellido2,
        nombre_encargado,
        fecha_baja,
        observacion_baja,
        estado
    FROM ESTUDIANTE
    WHERE estado = 0
    ORDER BY fecha_baja DESC;

END
GO




SELECT
id_estudiante,
nombre,
fecha_baja,
observacion_baja,
estado
FROM ESTUDIANTE
WHERE estado = 0;





EXEC SP_ListarEstudiantes;

SELECT DB_NAME() AS BaseActual;

SELECT COUNT(*) AS TotalEstudiantes

Select * FROM ESTUDIANTE;
Select * FROM ADECUACION_ACADEMICA;
Select * FROM TIPO_ADECUACION;
Select * FROM ROL;
Select * FROM USUARIO;