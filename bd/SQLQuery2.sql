
USE [AulaVirtualCopalchi];
GO

-- ============================================
-- 1. GUARDAR PLAN DE ESTUDIO
-- ============================================
CREATE OR ALTER PROCEDURE dbo.SP_GUARDAR_PLAN_ESTUDIO
    @id_docente INT,
    @id_grupo INT,
    @titulo NVARCHAR(200),
    @mes TINYINT,
    @anio INT,
    @nivel NVARCHAR(50),
    @asignatura NVARCHAR(100),
    @estado NVARCHAR(20),
    @observaciones NVARCHAR(1000),
    @archivo NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PLAN_ESTUDIO
    (
        id_docente,
        id_grupo,
        titulo,
        mes,
        anio,
        nivel,
        asignatura,
        estado,
        observaciones,
        fecha_carga,
        archivo
    )
    VALUES
    (
        @id_docente,
        @id_grupo,
        @titulo,
        @mes,
        @anio,
        @nivel,
        @asignatura,
        @estado,
        @observaciones,
        SYSDATETIME(),
        @archivo
    );
END;
GO

-- ============================================
-- 2. OBTENER TODOS LOS PLANES
-- ============================================
CREATE OR ALTER PROCEDURE dbo.SP_OBTENER_PLANES_ESTUDIO
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        P.id_plan,
        P.id_docente,
        P.id_grupo,
        P.titulo,
        P.mes,
        P.anio,
        P.nivel,
        P.asignatura,
        P.estado,
        P.observaciones,
        P.fecha_carga,
        P.fecha_revision,
        P.id_usuario_revision,
        P.archivo,
        CONCAT(
            U.nombre, ' ',
            U.apellido1, ' ',
            ISNULL(U.apellido2, '')
        ) AS nombre_docente
    FROM dbo.PLAN_ESTUDIO AS P
    INNER JOIN dbo.DOCENTE AS D
        ON P.id_docente = D.id_docente
    INNER JOIN dbo.USUARIO AS U
        ON D.id_usuario = U.id_usuario
    ORDER BY P.fecha_carga DESC;
END;
GO

-- ============================================
-- 3. OBTENER UN PLAN POR ID
-- ============================================
CREATE OR ALTER PROCEDURE dbo.SP_OBTENER_PLAN_ESTUDIO
    @id_plan INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        P.id_plan,
        P.id_docente,
        P.id_grupo,
        P.titulo,
        P.mes,
        P.anio,
        P.nivel,
        P.asignatura,
        P.estado,
        P.observaciones,
        P.archivo,
        P.fecha_carga,
        P.fecha_revision,
        P.id_usuario_revision,
        CONCAT(
            U.nombre, ' ',
            U.apellido1, ' ',
            ISNULL(U.apellido2, '')
        ) AS nombre_docente
    FROM dbo.PLAN_ESTUDIO AS P
    INNER JOIN dbo.DOCENTE AS D
        ON P.id_docente = D.id_docente
    INNER JOIN dbo.USUARIO AS U
        ON D.id_usuario = U.id_usuario
    WHERE P.id_plan = @id_plan;
END;
GO

-- ============================================
-- 4. ACTUALIZAR PLAN DE ESTUDIO
-- ============================================
CREATE OR ALTER PROCEDURE dbo.SP_ACTUALIZAR_PLAN_ESTUDIO
    @id_plan INT,
    @id_grupo INT,
    @titulo NVARCHAR(200),
    @mes TINYINT,
    @anio INT,
    @nivel NVARCHAR(50),
    @asignatura NVARCHAR(100),
    @archivo NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PLAN_ESTUDIO
    SET
        id_grupo = @id_grupo,
        titulo = @titulo,
        mes = @mes,
        anio = @anio,
        nivel = @nivel,
        asignatura = @asignatura,
        archivo = @archivo
    WHERE id_plan = @id_plan;
END;
GO

-- ============================================
-- 5. REVISAR PLAN DE ESTUDIO
-- ============================================
CREATE OR ALTER PROCEDURE dbo.SP_REVISAR_PLAN_ESTUDIO
    @id_plan INT,
    @estado NVARCHAR(20),
    @observaciones NVARCHAR(1000),
    @id_usuario_revision INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PLAN_ESTUDIO
    SET
        estado = @estado,
        observaciones = @observaciones,
        fecha_revision = SYSDATETIME(),
        id_usuario_revision = @id_usuario_revision
    WHERE id_plan = @id_plan;
END;
GO

-- ============================================
-- 6. ELIMINAR PLAN DE ESTUDIO
-- ============================================
CREATE OR ALTER PROCEDURE dbo.SP_ELIMINAR_PLAN_ESTUDIO
    @id_plan INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.PLAN_ESTUDIO
    WHERE id_plan = @id_plan;
END;
GO



USE AulaVirtualCopalchi;
GO

SELECT
    id_plan,
    id_docente,
    id_grupo,
    titulo,
    mes,
    anio,
    asignatura,
    nivel,
    estado,
    fecha_carga
FROM dbo.PLAN_ESTUDIO
ORDER BY fecha_carga DESC;
GO

EXEC dbo.SP_OBTENER_PLANES_ESTUDIO;
GO