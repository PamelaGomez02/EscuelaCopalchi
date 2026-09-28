/* =====================================================================
   MÓDULO: GESTIÓN ACADÉMICA
   Base de datos: AulaVirtualCopalchi
   ---------------------------------------------------------------------
   Se ejecuta DESPUÉS del script principal del proyecto (con PERMISO y
   ROL_PERMISO). Usa las tablas existentes: ROL, USUARIO, DOCENTE,
   ESTUDIANTE, AULA, GRUPO, MATRICULA, NOTIFICACION, AUDITORIA, PERMISO y
   ROL_PERMISO. Las reglas del negocio están en C# (Logica/GestionAcademica).
   Se puede ejecutar completo las veces que se necesite.
   ===================================================================== */

USE AulaVirtualCopalchi;
GO

/* =====================================================================
   1. CAMBIOS DE ESTRUCTURA
   ===================================================================== */

-- Datos del encargado que también usa el módulo de Estudiantes
IF COL_LENGTH(N'dbo.ESTUDIANTE', N'correo_encargado') IS NULL
    ALTER TABLE dbo.ESTUDIANTE ADD correo_encargado NVARCHAR(150) NULL;

IF COL_LENGTH(N'dbo.ESTUDIANTE', N'parentesco') IS NULL
    ALTER TABLE dbo.ESTUDIANTE ADD parentesco NVARCHAR(50) NULL;

-- Horario de cada grupo (una fila por día). GRUPO.horario guarda además el texto.
IF OBJECT_ID(N'dbo.HORARIO', N'U') IS NULL
    CREATE TABLE dbo.HORARIO
    (
        id_horario         INT IDENTITY(1,1) CONSTRAINT PK_HORARIO PRIMARY KEY,
        id_grupo           INT NOT NULL CONSTRAINT FK_HORARIO_GRUPO REFERENCES dbo.GRUPO(id_grupo),
        dia_semana         TINYINT NOT NULL CONSTRAINT CK_HORARIO_DIA CHECK (dia_semana BETWEEN 1 AND 7),  -- 1 = lunes
        hora_inicio        TIME(0) NOT NULL,
        hora_fin           TIME(0) NOT NULL,
        fecha_modificacion DATETIME2(0) NOT NULL CONSTRAINT DF_HORARIO_FECHA DEFAULT SYSDATETIME(),
        CONSTRAINT CK_HORARIO_HORAS CHECK (hora_fin > hora_inicio),
        CONSTRAINT UQ_HORARIO UNIQUE (id_grupo, dia_semana)
    );

-- Estado del envío por correo: Pendiente, Enviada o Error.
-- Las notificaciones de otros módulos quedan como "Enviada" (no se envían por correo).
IF COL_LENGTH(N'dbo.NOTIFICACION', N'estado_envio') IS NULL
    ALTER TABLE dbo.NOTIFICACION ADD estado_envio NVARCHAR(20) NOT NULL
        CONSTRAINT DF_NOTIFICACION_ENVIO DEFAULT N'Enviada' WITH VALUES;
GO

/* =====================================================================
   2. PERMISOS DEL MÓDULO (se administran luego en Roles → Permisos)
   ===================================================================== */

INSERT INTO dbo.PERMISO (nombre, descripcion, modulo)
SELECT v.nombre, v.descripcion, v.modulo
FROM (VALUES
        (N'GESTIONAR_ACADEMICA', N'Aulas, grupos, horarios, asignación de estudiantes, importación y bitácora', N'Gestión Académica'),
        (N'VER_MI_GRUPO',        N'Consultar sus grupos, estudiantes, aula y horario (docentes)',             N'Gestión Académica'),
        (N'VER_REPORTES',        N'Consultar y generar reportes',                                               N'Reportes')
     ) v (nombre, descripcion, modulo)
WHERE NOT EXISTS (SELECT 1 FROM dbo.PERMISO p WHERE p.nombre = v.nombre);

-- Permisos iniciales por rol (solo si el rol todavía no los tiene)
INSERT INTO dbo.ROL_PERMISO (id_rol, id_permiso)
SELECT r.id_rol, p.id_permiso
FROM (VALUES
        (N'Administrador', N'GESTIONAR_ACADEMICA'), (N'Administrador', N'VER_REPORTES'),
        (N'Director',      N'GESTIONAR_ACADEMICA'), (N'Director',      N'VER_REPORTES'),
        (N'Evaluación',    N'VER_REPORTES'),
        (N'Docente',       N'VER_MI_GRUPO')
     ) v (rol, permiso)
JOIN dbo.ROL r     ON r.nombre = v.rol
JOIN dbo.PERMISO p ON p.nombre = v.permiso
WHERE NOT EXISTS (SELECT 1 FROM dbo.ROL_PERMISO x WHERE x.id_rol = r.id_rol AND x.id_permiso = p.id_permiso);
GO

/* =====================================================================
   3. DATOS DE PRUEBA (opcional: se pueden borrar en producción)
   Un director y dos docentes. La contraseña la definirá el módulo de login.
   ===================================================================== */

INSERT INTO dbo.USUARIO (id_rol, identificacion, nombre, apellido1, apellido2, correo, telefono, contrasena_hash)
SELECT r.id_rol, v.identificacion, v.nombre, v.apellido1, v.apellido2, v.correo, v.telefono, N'PENDIENTE'
FROM (VALUES
        (N'Director', N'1-0000-0001', N'Laura', N'Méndez',   N'Solís',  N'director@copalchi.ed.cr', N'2222-0001'),
        (N'Docente',  N'1-1111-1111', N'María', N'González', N'Rojas',  N'maria.gonzalez@copalchi.ed.cr', N'8888-1111'),
        (N'Docente',  N'2-2222-2222', N'José',  N'Pérez',    N'Vargas', N'jose.perez@copalchi.ed.cr', N'8888-2222')
     ) v (rol, identificacion, nombre, apellido1, apellido2, correo, telefono)
JOIN dbo.ROL r ON r.nombre = v.rol
WHERE NOT EXISTS (SELECT 1 FROM dbo.USUARIO u WHERE u.identificacion = v.identificacion OR u.correo = v.correo);

-- Todo usuario con rol Docente debe estar en la tabla DOCENTE (GRUPO.id_docente apunta ahí)
INSERT INTO dbo.DOCENTE (id_usuario)
SELECT u.id_usuario
FROM dbo.USUARIO u JOIN dbo.ROL r ON r.id_rol = u.id_rol
WHERE r.nombre = N'Docente'
  AND NOT EXISTS (SELECT 1 FROM dbo.DOCENTE d WHERE d.id_usuario = u.id_usuario);

INSERT INTO dbo.AULA (nombre, capacidad, ubicacion)
SELECT v.nombre, v.capacidad, v.ubicacion
FROM (VALUES (N'Aula 1', 30, N'Pabellón A'), (N'Aula 2', 25, N'Pabellón A')) v (nombre, capacidad, ubicacion)
WHERE NOT EXISTS (SELECT 1 FROM dbo.AULA a WHERE a.nombre = v.nombre);
GO

/* =====================================================================
   4. USUARIOS Y DOCENTES
   ===================================================================== */

-- Usuarios con su rol, los permisos del rol (separados por coma) y su id de DOCENTE si está activo.
CREATE OR ALTER PROCEDURE dbo.SP_GA_Usuario_Listar
    @id_usuario INT = 0,
    @id_docente INT = 0
AS
    SELECT u.id_usuario, d.id_docente, u.identificacion,
           LTRIM(RTRIM(CONCAT(u.nombre, N' ', u.apellido1, N' ', u.apellido2))) AS nombre_completo,
           u.correo, u.telefono, r.nombre AS rol, u.estado,
           STUFF((SELECT N',' + p.nombre
                  FROM dbo.ROL_PERMISO rp
                  JOIN dbo.PERMISO p ON p.id_permiso = rp.id_permiso AND p.estado = 1
                  WHERE rp.id_rol = u.id_rol AND r.estado = 1
                  FOR XML PATH('')), 1, 1, N'') AS permisos
    FROM dbo.USUARIO u
    JOIN dbo.ROL r          ON r.id_rol = u.id_rol
    LEFT JOIN dbo.DOCENTE d ON d.id_usuario = u.id_usuario AND d.estado = 1
    WHERE (@id_usuario = 0 OR u.id_usuario = @id_usuario)
      AND (@id_docente = 0 OR d.id_docente = @id_docente)
    ORDER BY r.nombre, u.nombre, u.apellido1;
GO

/* =====================================================================
   5. AULAS
   ===================================================================== */

CREATE OR ALTER PROCEDURE dbo.SP_GA_Aula_Listar @id_aula INT = 0
AS
    SELECT id_aula, nombre, capacidad, ubicacion, estado
    FROM dbo.AULA
    WHERE @id_aula = 0 OR id_aula = @id_aula
    ORDER BY nombre;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GA_Aula_Insertar
    @nombre NVARCHAR(100), @capacidad INT, @ubicacion NVARCHAR(150) = NULL, @estado BIT = 1
AS
    INSERT INTO dbo.AULA (nombre, capacidad, ubicacion, estado) VALUES (@nombre, @capacidad, @ubicacion, @estado);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS id;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GA_Aula_Actualizar
    @id_aula INT, @nombre NVARCHAR(100), @capacidad INT, @ubicacion NVARCHAR(150) = NULL, @estado BIT = 1
AS
    UPDATE dbo.AULA
    SET nombre = @nombre, capacidad = @capacidad, ubicacion = @ubicacion, estado = @estado
    WHERE id_aula = @id_aula;
GO

/* =====================================================================
   6. GRUPOS Y HORARIOS
   ===================================================================== */

-- Filtros opcionales (0 = sin filtro). @anio = año del curso lectivo.
CREATE OR ALTER PROCEDURE dbo.SP_GA_Grupo_Listar
    @id_grupo INT = 0, @anio INT = 0, @id_docente INT = 0
AS
    SELECT g.id_grupo, g.nombre, g.nivel, g.anio, g.periodo, g.capacidad AS cupo, g.estado,
           g.id_aula, a.nombre AS aula, a.capacidad AS capacidad_aula, a.ubicacion,
           g.id_docente, d.id_usuario AS id_usuario_docente,
           LTRIM(RTRIM(CONCAT(u.nombre, N' ', u.apellido1, N' ', u.apellido2))) AS docente,
           (SELECT COUNT(*) FROM dbo.MATRICULA m WHERE m.id_grupo = g.id_grupo AND m.estado = N'Activa') AS total_estudiantes
    FROM dbo.GRUPO g
    LEFT JOIN dbo.AULA a    ON a.id_aula = g.id_aula
    LEFT JOIN dbo.DOCENTE d ON d.id_docente = g.id_docente
    LEFT JOIN dbo.USUARIO u ON u.id_usuario = d.id_usuario
    WHERE (@id_grupo = 0 OR g.id_grupo = @id_grupo)
      AND (@anio = 0 OR g.anio = @anio)
      AND (@id_docente = 0 OR g.id_docente = @id_docente)
    ORDER BY g.anio DESC, g.nombre;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GA_Grupo_Insertar
    @nombre NVARCHAR(100), @nivel NVARCHAR(50) = NULL, @anio INT, @capacidad INT,
    @horario NVARCHAR(150) = NULL, @id_aula INT = NULL, @id_docente INT = NULL
AS
    INSERT INTO dbo.GRUPO (nombre, nivel, anio, periodo, capacidad, horario, id_aula, id_docente)
    VALUES (@nombre, ISNULL(@nivel, N''), @anio, CAST(@anio AS NVARCHAR(20)), @capacidad, @horario, @id_aula, @id_docente);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS id;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GA_Grupo_Actualizar
    @id_grupo INT, @nombre NVARCHAR(100), @nivel NVARCHAR(50) = NULL, @anio INT, @capacidad INT,
    @horario NVARCHAR(150) = NULL, @id_aula INT = NULL, @id_docente INT = NULL, @estado BIT = 1
AS
    UPDATE dbo.GRUPO
    SET nombre = @nombre, nivel = ISNULL(@nivel, N''), anio = @anio, periodo = CAST(@anio AS NVARCHAR(20)),
        capacidad = @capacidad, horario = @horario, id_aula = @id_aula, id_docente = @id_docente, estado = @estado
    WHERE id_grupo = @id_grupo;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GA_Horario_Listar @id_grupo INT = 0
AS
    SELECT id_grupo, dia_semana, hora_inicio, hora_fin, fecha_modificacion
    FROM dbo.HORARIO
    WHERE @id_grupo = 0 OR id_grupo = @id_grupo
    ORDER BY id_grupo, dia_semana;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GA_Horario_Eliminar @id_grupo INT
AS
    DELETE FROM dbo.HORARIO WHERE id_grupo = @id_grupo;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GA_Horario_Insertar
    @id_grupo INT, @dia_semana TINYINT, @hora_inicio TIME(0), @hora_fin TIME(0)
AS
    INSERT INTO dbo.HORARIO (id_grupo, dia_semana, hora_inicio, hora_fin)
    VALUES (@id_grupo, @dia_semana, @hora_inicio, @hora_fin);
GO

/* =====================================================================
   7. MATRÍCULA (asignación de estudiantes a grupos)
   ===================================================================== */

-- Estudiantes activos con su matrícula activa (si tienen).
-- @id_grupo: 0 = todos, -1 = solo sin grupo.  @id_estudiante: 0 = todos.
CREATE OR ALTER PROCEDURE dbo.SP_GA_Matricula_Listar
    @id_grupo INT = 0, @id_estudiante INT = 0, @busqueda NVARCHAR(100) = N''
AS
    SELECT e.id_estudiante, e.identificacion,
           LTRIM(RTRIM(CONCAT(e.nombre, N' ', e.apellido1, N' ', e.apellido2))) AS nombre_completo,
           e.fecha_nacimiento, e.direccion, e.nombre_encargado, e.parentesco,
           e.telefono_encargado, e.correo_encargado, e.estado,
           m.id_matricula, m.fecha AS fecha_asignacion,
           g.id_grupo, g.nombre AS grupo, a.nombre AS aula,
           LTRIM(RTRIM(CONCAT(u.nombre, N' ', u.apellido1, N' ', u.apellido2))) AS docente
    FROM dbo.ESTUDIANTE e
    LEFT JOIN dbo.MATRICULA m ON m.id_estudiante = e.id_estudiante AND m.estado = N'Activa'
    LEFT JOIN dbo.GRUPO g     ON g.id_grupo = m.id_grupo
    LEFT JOIN dbo.AULA a      ON a.id_aula = g.id_aula
    LEFT JOIN dbo.DOCENTE d   ON d.id_docente = g.id_docente
    LEFT JOIN dbo.USUARIO u   ON u.id_usuario = d.id_usuario
    WHERE e.estado = 1
      AND (@id_estudiante = 0 OR e.id_estudiante = @id_estudiante)
      AND (@id_grupo = 0 OR (@id_grupo = -1 AND m.id_matricula IS NULL) OR g.id_grupo = @id_grupo)
      AND (ISNULL(@busqueda, N'') = N''
           OR CONCAT(e.nombre, N' ', e.apellido1, N' ', e.apellido2) LIKE N'%' + @busqueda + N'%'
           OR e.identificacion LIKE N'%' + @busqueda + N'%')
    ORDER BY e.apellido1, e.apellido2, e.nombre;
GO

-- Matricula al estudiante en el grupo. Si ya estuvo en ese grupo y período, reactiva ese registro
-- (MATRICULA no permite repetir estudiante + grupo + período).
CREATE OR ALTER PROCEDURE dbo.SP_GA_Matricula_Activar
    @id_estudiante INT, @id_grupo INT
AS
    DECLARE @periodo NVARCHAR(20) = (SELECT periodo FROM dbo.GRUPO WHERE id_grupo = @id_grupo);

    UPDATE dbo.MATRICULA
    SET estado = N'Activa', fecha = CAST(GETDATE() AS DATE)
    WHERE id_estudiante = @id_estudiante AND id_grupo = @id_grupo AND periodo = @periodo;

    IF @@ROWCOUNT = 0
        INSERT INTO dbo.MATRICULA (id_estudiante, id_grupo, periodo) VALUES (@id_estudiante, @id_grupo, @periodo);
GO

-- @estado: 'Trasladada', 'Retirada' o 'Finalizada'
CREATE OR ALTER PROCEDURE dbo.SP_GA_Matricula_CambiarEstado
    @id_matricula INT, @estado NVARCHAR(20)
AS
    UPDATE dbo.MATRICULA SET estado = @estado WHERE id_matricula = @id_matricula;
GO

/* =====================================================================
   8. BITÁCORA E INCIDENTES (tabla AUDITORIA)
   ===================================================================== */

CREATE OR ALTER PROCEDURE dbo.SP_GA_Auditoria_Insertar
    @id_usuario INT = NULL, @accion NVARCHAR(100), @modulo NVARCHAR(100),
    @tabla_afectada NVARCHAR(100) = NULL, @registro_afectado NVARCHAR(100) = NULL, @descripcion NVARCHAR(1000) = NULL
AS
    INSERT INTO dbo.AUDITORIA (id_usuario, accion, modulo, tabla_afectada, registro_afectado, descripcion)
    VALUES (@id_usuario, @accion, @modulo, @tabla_afectada, @registro_afectado, @descripcion);
GO

CREATE OR ALTER PROCEDURE dbo.SP_GA_Auditoria_Listar
    @modulo NVARCHAR(100), @accion NVARCHAR(100) = N'', @desde DATE = NULL, @hasta DATE = NULL
AS
    SELECT TOP (500) a.id_auditoria, a.fecha, a.accion, a.descripcion, a.tabla_afectada, a.registro_afectado,
           LTRIM(RTRIM(CONCAT(u.nombre, N' ', u.apellido1))) AS responsable
    FROM dbo.AUDITORIA a
    LEFT JOIN dbo.USUARIO u ON u.id_usuario = a.id_usuario
    WHERE a.modulo = @modulo
      AND (ISNULL(@accion, N'') = N'' OR a.accion = @accion)
      AND (@desde IS NULL OR CAST(a.fecha AS DATE) >= @desde)
      AND (@hasta IS NULL OR CAST(a.fecha AS DATE) <= @hasta)
    ORDER BY a.fecha DESC, a.id_auditoria DESC;
GO

/* =====================================================================
   9. NOTIFICACIONES
   ===================================================================== */

CREATE OR ALTER PROCEDURE dbo.SP_GA_Notificacion_Insertar
    @id_usuario INT, @tipo NVARCHAR(50), @titulo NVARCHAR(200), @mensaje NVARCHAR(MAX)
AS
    INSERT INTO dbo.NOTIFICACION (id_usuario, tipo, titulo, mensaje, estado_envio)
    VALUES (@id_usuario, @tipo, @titulo, @mensaje, N'Pendiente');
GO

-- @id_usuario = 0 -> todos.  @estado_envio = '' -> todos
CREATE OR ALTER PROCEDURE dbo.SP_GA_Notificacion_Listar
    @id_usuario INT = 0, @solo_no_leidas BIT = 0, @estado_envio NVARCHAR(20) = N''
AS
    SELECT TOP (300) n.id_notificacion, n.id_usuario, n.tipo, n.titulo, n.mensaje, n.fecha,
           n.leida, n.estado_envio, u.correo,
           LTRIM(RTRIM(CONCAT(u.nombre, N' ', u.apellido1))) AS destinatario
    FROM dbo.NOTIFICACION n
    JOIN dbo.USUARIO u ON u.id_usuario = n.id_usuario
    WHERE (@id_usuario = 0 OR n.id_usuario = @id_usuario)
      AND (@solo_no_leidas = 0 OR n.leida = 0)
      AND (ISNULL(@estado_envio, N'') = N'' OR n.estado_envio = @estado_envio)
    ORDER BY n.fecha DESC, n.id_notificacion DESC;
GO

-- @id_notificacion = 0 -> todas las del usuario
CREATE OR ALTER PROCEDURE dbo.SP_GA_Notificacion_MarcarLeida
    @id_notificacion INT = 0, @id_usuario INT
AS
    UPDATE dbo.NOTIFICACION
    SET leida = 1, fecha_lectura = SYSDATETIME()
    WHERE id_usuario = @id_usuario AND leida = 0
      AND (@id_notificacion = 0 OR id_notificacion = @id_notificacion);
GO

CREATE OR ALTER PROCEDURE dbo.SP_GA_Notificacion_ActualizarEnvio
    @id_notificacion INT, @estado_envio NVARCHAR(20)
AS
    UPDATE dbo.NOTIFICACION SET estado_envio = @estado_envio WHERE id_notificacion = @id_notificacion;
GO

/* =====================================================================
   VERIFICACIÓN: permisos de Gestión Académica por rol
   ===================================================================== */

SELECT r.nombre AS rol, p.nombre AS permiso
FROM dbo.ROL_PERMISO rp
JOIN dbo.ROL r     ON r.id_rol = rp.id_rol
JOIN dbo.PERMISO p ON p.id_permiso = rp.id_permiso
WHERE p.nombre IN (N'GESTIONAR_ACADEMICA', N'VER_MI_GRUPO', N'VER_REPORTES')
ORDER BY r.nombre, p.nombre;
GO