
/* ============================================================
   PROYECTO: AULA VIRTUAL COPALCHÍ
   MOTOR: Microsoft SQL Server
   ============================================================ */

---------------------------------------------------------------
-- 1. CREAR BASE DE DATOS
---------------------------------------------------------------

IF DB_ID('AulaVirtualCopalchi') IS NULL
BEGIN
    CREATE DATABASE AulaVirtualCopalchi;
END
GO

USE AulaVirtualCopalchi;
GO


/* ============================================================
   TABLA: ROL
   ============================================================ */

CREATE TABLE ROL
(
    id_rol INT IDENTITY(1,1) NOT NULL,
    nombre NVARCHAR(50) NOT NULL,
    descripcion NVARCHAR(250) NULL,
    estado BIT NOT NULL CONSTRAINT DF_ROL_ESTADO DEFAULT 1,

    CONSTRAINT PK_ROL PRIMARY KEY (id_rol),
    CONSTRAINT UQ_ROL_NOMBRE UNIQUE (nombre)
);
GO


/* ============================================================
   TABLA: USUARIO
   ============================================================ */

CREATE TABLE USUARIO
(
    id_usuario INT IDENTITY(1,1) NOT NULL,
    id_rol INT NOT NULL,

    identificacion NVARCHAR(30) NOT NULL,
    nombre NVARCHAR(80) NOT NULL,
    apellido1 NVARCHAR(80) NOT NULL,
    apellido2 NVARCHAR(80) NULL,

    correo NVARCHAR(150) NOT NULL,
    telefono NVARCHAR(25) NULL,

    -- Nunca almacenar la contraseña en texto plano.
    contrasena_hash NVARCHAR(255) NOT NULL,

    estado BIT NOT NULL
        CONSTRAINT DF_USUARIO_ESTADO DEFAULT 1,

    ultimo_acceso DATETIME2 NULL,

    fecha_creacion DATETIME2 NOT NULL
        CONSTRAINT DF_USUARIO_FECHA_CREACION DEFAULT SYSDATETIME(),

    CONSTRAINT PK_USUARIO
        PRIMARY KEY (id_usuario),

    CONSTRAINT FK_USUARIO_ROL
        FOREIGN KEY (id_rol)
        REFERENCES ROL(id_rol),

    CONSTRAINT UQ_USUARIO_IDENTIFICACION
        UNIQUE (identificacion),

    CONSTRAINT UQ_USUARIO_CORREO
        UNIQUE (correo)
);
GO


/* ============================================================
   TABLA: DOCENTE
   ============================================================ */

CREATE TABLE DOCENTE
(
    id_docente INT IDENTITY(1,1) NOT NULL,
    id_usuario INT NOT NULL,

    especialidad NVARCHAR(150) NULL,
    grado_academico NVARCHAR(100) NULL,
    fecha_ingreso DATE NULL,

    estado BIT NOT NULL
        CONSTRAINT DF_DOCENTE_ESTADO DEFAULT 1,

    CONSTRAINT PK_DOCENTE
        PRIMARY KEY (id_docente),

    CONSTRAINT FK_DOCENTE_USUARIO
        FOREIGN KEY (id_usuario)
        REFERENCES USUARIO(id_usuario),

    CONSTRAINT UQ_DOCENTE_USUARIO
        UNIQUE (id_usuario)
);
GO


/* ============================================================
   TABLA: ESTUDIANTE
   ============================================================ */

CREATE TABLE ESTUDIANTE
(
    id_estudiante INT IDENTITY(1,1) NOT NULL,

    identificacion NVARCHAR(30) NULL,

    nombre NVARCHAR(80) NOT NULL,
    apellido1 NVARCHAR(80) NOT NULL,
    apellido2 NVARCHAR(80) NULL,

    fecha_nacimiento DATE NULL,
    direccion NVARCHAR(300) NULL,

    nombre_encargado NVARCHAR(150) NULL,
    telefono_encargado NVARCHAR(25) NULL,

    estado BIT NOT NULL
        CONSTRAINT DF_ESTUDIANTE_ESTADO DEFAULT 1,

    fecha_registro DATETIME2 NOT NULL
        CONSTRAINT DF_ESTUDIANTE_FECHA_REGISTRO DEFAULT SYSDATETIME(),

    CONSTRAINT PK_ESTUDIANTE
        PRIMARY KEY (id_estudiante),

    CONSTRAINT UQ_ESTUDIANTE_IDENTIFICACION
        UNIQUE (identificacion)
);
GO


/* ============================================================
   TABLA: AULA
   ============================================================ */

CREATE TABLE AULA
(
    id_aula INT IDENTITY(1,1) NOT NULL,

    nombre NVARCHAR(100) NOT NULL,
    capacidad INT NOT NULL,
    ubicacion NVARCHAR(150) NULL,

    estado BIT NOT NULL
        CONSTRAINT DF_AULA_ESTADO DEFAULT 1,

    CONSTRAINT PK_AULA
        PRIMARY KEY (id_aula),

    CONSTRAINT UQ_AULA_NOMBRE
        UNIQUE (nombre),

    CONSTRAINT CK_AULA_CAPACIDAD
        CHECK (capacidad > 0)
);
GO


/* ============================================================
   TABLA: GRUPO
   ============================================================ */

CREATE TABLE GRUPO
(
    id_grupo INT IDENTITY(1,1) NOT NULL,

    id_aula INT NULL,
    id_docente INT NULL,

    nombre NVARCHAR(100) NOT NULL,
    nivel NVARCHAR(50) NOT NULL,

    horario NVARCHAR(150) NULL,
    capacidad INT NOT NULL,

    periodo NVARCHAR(20) NOT NULL,
    anio INT NOT NULL,

    estado BIT NOT NULL
        CONSTRAINT DF_GRUPO_ESTADO DEFAULT 1,

    CONSTRAINT PK_GRUPO
        PRIMARY KEY (id_grupo),

    CONSTRAINT FK_GRUPO_AULA
        FOREIGN KEY (id_aula)
        REFERENCES AULA(id_aula),

    CONSTRAINT FK_GRUPO_DOCENTE
        FOREIGN KEY (id_docente)
        REFERENCES DOCENTE(id_docente),

    CONSTRAINT CK_GRUPO_CAPACIDAD
        CHECK (capacidad > 0),

    CONSTRAINT CK_GRUPO_ANIO
        CHECK (anio >= 2000)
);
GO


/* ============================================================
   TABLA: MATRICULA
   ============================================================ */

CREATE TABLE MATRICULA
(
    id_matricula INT IDENTITY(1,1) NOT NULL,

    id_estudiante INT NOT NULL,
    id_grupo INT NOT NULL,

    fecha DATE NOT NULL
        CONSTRAINT DF_MATRICULA_FECHA DEFAULT CAST(GETDATE() AS DATE),

    estado NVARCHAR(20) NOT NULL
        CONSTRAINT DF_MATRICULA_ESTADO DEFAULT 'Activa',

    periodo NVARCHAR(20) NOT NULL,

    CONSTRAINT PK_MATRICULA
        PRIMARY KEY (id_matricula),

    CONSTRAINT FK_MATRICULA_ESTUDIANTE
        FOREIGN KEY (id_estudiante)
        REFERENCES ESTUDIANTE(id_estudiante),

    CONSTRAINT FK_MATRICULA_GRUPO
        FOREIGN KEY (id_grupo)
        REFERENCES GRUPO(id_grupo),

    CONSTRAINT CK_MATRICULA_ESTADO
        CHECK (estado IN
        (
            'Activa',
            'Retirada',
            'Trasladada',
            'Finalizada'
        )),

    CONSTRAINT UQ_MATRICULA
        UNIQUE (id_estudiante, id_grupo, periodo)
);
GO


/* ============================================================
   TABLA: ASISTENCIA
   ============================================================ */

CREATE TABLE ASISTENCIA
(
    id_asistencia INT IDENTITY(1,1) NOT NULL,

    id_estudiante INT NOT NULL,
    id_grupo INT NOT NULL,
    id_docente INT NOT NULL,

    fecha DATE NOT NULL,

    estado NVARCHAR(20) NOT NULL,

    observacion NVARCHAR(500) NULL,

    fecha_registro DATETIME2 NOT NULL
        CONSTRAINT DF_ASISTENCIA_REGISTRO DEFAULT SYSDATETIME(),

    CONSTRAINT PK_ASISTENCIA
        PRIMARY KEY (id_asistencia),

    CONSTRAINT FK_ASISTENCIA_ESTUDIANTE
        FOREIGN KEY (id_estudiante)
        REFERENCES ESTUDIANTE(id_estudiante),

    CONSTRAINT FK_ASISTENCIA_GRUPO
        FOREIGN KEY (id_grupo)
        REFERENCES GRUPO(id_grupo),

    CONSTRAINT FK_ASISTENCIA_DOCENTE
        FOREIGN KEY (id_docente)
        REFERENCES DOCENTE(id_docente),

    CONSTRAINT CK_ASISTENCIA_ESTADO
        CHECK (estado IN
        (
            'Presente',
            'Ausente',
            'Justificado',
            'Tardanza'
        )),

    -- Impide registrar dos asistencias del mismo estudiante
    -- para el mismo grupo en la misma fecha.
    CONSTRAINT UQ_ASISTENCIA
        UNIQUE (id_estudiante, id_grupo, fecha)
);
GO


/* ============================================================
   TABLA: PLAN_ESTUDIO
   ============================================================ */

CREATE TABLE PLAN_ESTUDIO
(
    id_plan INT IDENTITY(1,1) NOT NULL,

    id_docente INT NOT NULL,
    id_grupo INT NOT NULL,

    titulo NVARCHAR(200) NOT NULL,

    archivo NVARCHAR(500) NULL,

    mes TINYINT NOT NULL,
    anio INT NOT NULL,

    estado NVARCHAR(20) NOT NULL
        CONSTRAINT DF_PLAN_ESTADO DEFAULT 'Pendiente',

    observaciones NVARCHAR(1000) NULL,

    fecha_carga DATETIME2 NOT NULL
        CONSTRAINT DF_PLAN_FECHA_CARGA DEFAULT SYSDATETIME(),

    fecha_revision DATETIME2 NULL,

    id_usuario_revision INT NULL,

    CONSTRAINT PK_PLAN_ESTUDIO
        PRIMARY KEY (id_plan),

    CONSTRAINT FK_PLAN_DOCENTE
        FOREIGN KEY (id_docente)
        REFERENCES DOCENTE(id_docente),

    CONSTRAINT FK_PLAN_GRUPO
        FOREIGN KEY (id_grupo)
        REFERENCES GRUPO(id_grupo),

    CONSTRAINT FK_PLAN_USUARIO_REVISION
        FOREIGN KEY (id_usuario_revision)
        REFERENCES USUARIO(id_usuario),

    CONSTRAINT CK_PLAN_MES
        CHECK (mes BETWEEN 1 AND 12),

    CONSTRAINT CK_PLAN_ANIO
        CHECK (anio >= 2000),

    CONSTRAINT CK_PLAN_ESTADO
        CHECK (estado IN
        (
            'Pendiente',
            'Aprobado',
            'Denegado'
        ))
);
GO


/* ============================================================
   TABLA: MATERIAL_DIDACTICO
   ============================================================ */

CREATE TABLE MATERIAL_DIDACTICO
(
    id_material INT IDENTITY(1,1) NOT NULL,

    id_docente INT NOT NULL,
    id_grupo INT NULL,

    titulo NVARCHAR(200) NOT NULL,

    tipo NVARCHAR(20) NOT NULL,

    archivo NVARCHAR(500) NULL,

    url NVARCHAR(1000) NULL,

    descripcion NVARCHAR(1000) NULL,

    fecha_carga DATETIME2 NOT NULL
        CONSTRAINT DF_MATERIAL_FECHA DEFAULT SYSDATETIME(),

    estado BIT NOT NULL
        CONSTRAINT DF_MATERIAL_ESTADO DEFAULT 1,

    CONSTRAINT PK_MATERIAL_DIDACTICO
        PRIMARY KEY (id_material),

    CONSTRAINT FK_MATERIAL_DOCENTE
        FOREIGN KEY (id_docente)
        REFERENCES DOCENTE(id_docente),

    CONSTRAINT FK_MATERIAL_GRUPO
        FOREIGN KEY (id_grupo)
        REFERENCES GRUPO(id_grupo),

    CONSTRAINT CK_MATERIAL_TIPO
        CHECK (tipo IN
        (
            'PDF',
            'PPTX',
            'DOCX',
            'XLSX',
            'MP4',
            'JPG',
            'JPEG',
            'PNG',
            'URL',
            'OTRO'
        ))
);
GO


/* ============================================================
   TABLA: EVALUACION
   ============================================================ */

CREATE TABLE EVALUACION
(
    id_evaluacion INT IDENTITY(1,1) NOT NULL,

    id_grupo INT NOT NULL,

    nombre NVARCHAR(150) NOT NULL,

    porcentaje DECIMAL(5,2) NOT NULL,

    fecha DATE NULL,

    nota_maxima DECIMAL(6,2) NOT NULL
        CONSTRAINT DF_EVALUACION_NOTA_MAXIMA DEFAULT 100,

    estado BIT NOT NULL
        CONSTRAINT DF_EVALUACION_ESTADO DEFAULT 1,

    CONSTRAINT PK_EVALUACION
        PRIMARY KEY (id_evaluacion),

    CONSTRAINT FK_EVALUACION_GRUPO
        FOREIGN KEY (id_grupo)
        REFERENCES GRUPO(id_grupo),

    CONSTRAINT CK_EVALUACION_PORCENTAJE
        CHECK (porcentaje > 0 AND porcentaje <= 100),

    CONSTRAINT CK_EVALUACION_NOTA_MAXIMA
        CHECK (nota_maxima > 0)
);
GO


/* ============================================================
   TABLA: CALIFICACION
   ============================================================ */

CREATE TABLE CALIFICACION
(
    id_calificacion INT IDENTITY(1,1) NOT NULL,

    id_evaluacion INT NOT NULL,
    id_estudiante INT NOT NULL,

    nota DECIMAL(6,2) NOT NULL,

    observacion NVARCHAR(500) NULL,

    fecha_registro DATETIME2 NOT NULL
        CONSTRAINT DF_CALIFICACION_FECHA DEFAULT SYSDATETIME(),

    CONSTRAINT PK_CALIFICACION
        PRIMARY KEY (id_calificacion),

    CONSTRAINT FK_CALIFICACION_EVALUACION
        FOREIGN KEY (id_evaluacion)
        REFERENCES EVALUACION(id_evaluacion),

    CONSTRAINT FK_CALIFICACION_ESTUDIANTE
        FOREIGN KEY (id_estudiante)
        REFERENCES ESTUDIANTE(id_estudiante),

    CONSTRAINT CK_CALIFICACION_NOTA
        CHECK (nota >= 0),

    -- Un estudiante solo puede tener una nota
    -- por evaluación.
    CONSTRAINT UQ_CALIFICACION
        UNIQUE (id_evaluacion, id_estudiante)
);
GO


/* ============================================================
   TABLA: NOTIFICACION
   ============================================================ */

CREATE TABLE NOTIFICACION
(
    id_notificacion INT IDENTITY(1,1) NOT NULL,

    id_usuario INT NOT NULL,

    tipo NVARCHAR(50) NOT NULL,

    titulo NVARCHAR(200) NOT NULL,

    mensaje NVARCHAR(MAX) NOT NULL,

    fecha DATETIME2 NOT NULL
        CONSTRAINT DF_NOTIFICACION_FECHA DEFAULT SYSDATETIME(),

    leida BIT NOT NULL
        CONSTRAINT DF_NOTIFICACION_LEIDA DEFAULT 0,

    fecha_lectura DATETIME2 NULL,

    CONSTRAINT PK_NOTIFICACION
        PRIMARY KEY (id_notificacion),

    CONSTRAINT FK_NOTIFICACION_USUARIO
        FOREIGN KEY (id_usuario)
        REFERENCES USUARIO(id_usuario)
);
GO


/* ============================================================
   TABLA: AUDITORIA
   ============================================================ */

CREATE TABLE AUDITORIA
(
    id_auditoria BIGINT IDENTITY(1,1) NOT NULL,

    id_usuario INT NULL,

    accion NVARCHAR(100) NOT NULL,

    modulo NVARCHAR(100) NOT NULL,

    tabla_afectada NVARCHAR(100) NULL,

    registro_afectado NVARCHAR(100) NULL,

    descripcion NVARCHAR(1000) NULL,

    direccion_ip NVARCHAR(45) NULL,

    fecha DATETIME2 NOT NULL
        CONSTRAINT DF_AUDITORIA_FECHA DEFAULT SYSDATETIME(),

    CONSTRAINT PK_AUDITORIA
        PRIMARY KEY (id_auditoria),

    CONSTRAINT FK_AUDITORIA_USUARIO
        FOREIGN KEY (id_usuario)
        REFERENCES USUARIO(id_usuario)
);
GO


/* ============================================================
   TABLA: INVENTARIO
   ============================================================ */

CREATE TABLE INVENTARIO
(
    id_producto INT IDENTITY(1,1) NOT NULL,

    nombre NVARCHAR(150) NOT NULL,

    stock_actual INT NOT NULL
        CONSTRAINT DF_INVENTARIO_STOCK DEFAULT 0,

    stock_minimo INT NOT NULL
        CONSTRAINT DF_INVENTARIO_MINIMO DEFAULT 0,

    unidad_medida NVARCHAR(50) NOT NULL,

    categoria NVARCHAR(100) NULL,

    fecha_vencimiento DATE NULL,

    estado BIT NOT NULL
        CONSTRAINT DF_INVENTARIO_ESTADO DEFAULT 1,

    CONSTRAINT PK_INVENTARIO
        PRIMARY KEY (id_producto),

    CONSTRAINT CK_INVENTARIO_STOCK
        CHECK (stock_actual >= 0),

    CONSTRAINT CK_INVENTARIO_MINIMO
        CHECK (stock_minimo >= 0)
);
GO


/* ============================================================
   TABLA: MOVIMIENTO_INVENTARIO
   ============================================================ */

CREATE TABLE MOVIMIENTO_INVENTARIO
(
    id_movimiento BIGINT IDENTITY(1,1) NOT NULL,

    id_producto INT NOT NULL,
    id_usuario INT NOT NULL,

    tipo NVARCHAR(20) NOT NULL,

    cantidad INT NOT NULL,

    fecha DATETIME2 NOT NULL
        CONSTRAINT DF_MOVIMIENTO_FECHA DEFAULT SYSDATETIME(),

    motivo NVARCHAR(500) NULL,

    CONSTRAINT PK_MOVIMIENTO_INVENTARIO
        PRIMARY KEY (id_movimiento),

    CONSTRAINT FK_MOVIMIENTO_PRODUCTO
        FOREIGN KEY (id_producto)
        REFERENCES INVENTARIO(id_producto),

    CONSTRAINT FK_MOVIMIENTO_USUARIO
        FOREIGN KEY (id_usuario)
        REFERENCES USUARIO(id_usuario),

    CONSTRAINT CK_MOVIMIENTO_TIPO
        CHECK (tipo IN
        (
            'Entrada',
            'Salida',
            'Ajuste'
        )),

    CONSTRAINT CK_MOVIMIENTO_CANTIDAD
        CHECK (cantidad > 0)
);
GO


/* ============================================================
   TABLA: ADECUACION_ACADEMICA
   ============================================================ */

CREATE TABLE ADECUACION_ACADEMICA
(
    id_adecuacion INT IDENTITY(1,1) NOT NULL,

    id_estudiante INT NOT NULL,
    id_usuario_registro INT NOT NULL,

    tipo NVARCHAR(100) NOT NULL,

    descripcion NVARCHAR(1500) NOT NULL,

    medidas NVARCHAR(1500) NULL,

    fecha_inicio DATE NOT NULL,

    fecha_fin DATE NULL,

    estado NVARCHAR(20) NOT NULL
        CONSTRAINT DF_ADECUACION_ESTADO DEFAULT 'Activa',

    fecha_registro DATETIME2 NOT NULL
        CONSTRAINT DF_ADECUACION_REGISTRO DEFAULT SYSDATETIME(),

    CONSTRAINT PK_ADECUACION_ACADEMICA
        PRIMARY KEY (id_adecuacion),

    CONSTRAINT FK_ADECUACION_ESTUDIANTE
        FOREIGN KEY (id_estudiante)
        REFERENCES ESTUDIANTE(id_estudiante),

    CONSTRAINT FK_ADECUACION_USUARIO
        FOREIGN KEY (id_usuario_registro)
        REFERENCES USUARIO(id_usuario),

    CONSTRAINT CK_ADECUACION_ESTADO
        CHECK (estado IN
        (
            'Activa',
            'Finalizada',
            'Suspendida'
        )),

    CONSTRAINT CK_ADECUACION_FECHAS
        CHECK (fecha_fin IS NULL OR fecha_fin >= fecha_inicio)
);
GO


/* ============================================================
   TABLA: SEGUIMIENTO_ADECUACION
   ============================================================ */

CREATE TABLE SEGUIMIENTO_ADECUACION
(
    id_seguimiento INT IDENTITY(1,1) NOT NULL,

    id_adecuacion INT NOT NULL,
    id_usuario INT NOT NULL,

    fecha DATETIME2 NOT NULL
        CONSTRAINT DF_SEGUIMIENTO_FECHA DEFAULT SYSDATETIME(),

    observacion NVARCHAR(1500) NOT NULL,

    CONSTRAINT PK_SEGUIMIENTO_ADECUACION
        PRIMARY KEY (id_seguimiento),

    CONSTRAINT FK_SEGUIMIENTO_ADECUACION
        FOREIGN KEY (id_adecuacion)
        REFERENCES ADECUACION_ACADEMICA(id_adecuacion),

    CONSTRAINT FK_SEGUIMIENTO_USUARIO
        FOREIGN KEY (id_usuario)
        REFERENCES USUARIO(id_usuario)
);
GO


/* ============================================================
   TABLA: ALERTA_ACADEMICA
   ============================================================ */

CREATE TABLE ALERTA_ACADEMICA
(
    id_alerta INT IDENTITY(1,1) NOT NULL,

    id_estudiante INT NOT NULL,
    id_grupo INT NULL,

    tipo NVARCHAR(50) NOT NULL,

    descripcion NVARCHAR(500) NOT NULL,

    fecha DATETIME2 NOT NULL
        CONSTRAINT DF_ALERTA_FECHA DEFAULT SYSDATETIME(),

    atendida BIT NOT NULL
        CONSTRAINT DF_ALERTA_ATENDIDA DEFAULT 0,

    CONSTRAINT PK_ALERTA_ACADEMICA
        PRIMARY KEY (id_alerta),

    CONSTRAINT FK_ALERTA_ESTUDIANTE
        FOREIGN KEY (id_estudiante)
        REFERENCES ESTUDIANTE(id_estudiante),

    CONSTRAINT FK_ALERTA_GRUPO
        FOREIGN KEY (id_grupo)
        REFERENCES GRUPO(id_grupo)
);
GO


/* ============================================================
   ÍNDICES
   Mejoran búsquedas y consultas frecuentes.
   ============================================================ */

CREATE INDEX IX_USUARIO_ROL
ON USUARIO(id_rol);
GO

CREATE INDEX IX_GRUPO_DOCENTE
ON GRUPO(id_docente);
GO

CREATE INDEX IX_GRUPO_AULA
ON GRUPO(id_aula);
GO

CREATE INDEX IX_MATRICULA_ESTUDIANTE
ON MATRICULA(id_estudiante);
GO

CREATE INDEX IX_MATRICULA_GRUPO
ON MATRICULA(id_grupo);
GO

CREATE INDEX IX_ASISTENCIA_FECHA
ON ASISTENCIA(fecha);
GO

CREATE INDEX IX_ASISTENCIA_GRUPO
ON ASISTENCIA(id_grupo);
GO

CREATE INDEX IX_PLAN_DOCENTE
ON PLAN_ESTUDIO(id_docente);
GO

CREATE INDEX IX_PLAN_GRUPO
ON PLAN_ESTUDIO(id_grupo);
GO

CREATE INDEX IX_MATERIAL_GRUPO
ON MATERIAL_DIDACTICO(id_grupo);
GO

CREATE INDEX IX_EVALUACION_GRUPO
ON EVALUACION(id_grupo);
GO

CREATE INDEX IX_CALIFICACION_ESTUDIANTE
ON CALIFICACION(id_estudiante);
GO

CREATE INDEX IX_NOTIFICACION_USUARIO
ON NOTIFICACION(id_usuario, leida);
GO

CREATE INDEX IX_AUDITORIA_USUARIO
ON AUDITORIA(id_usuario);
GO

CREATE INDEX IX_AUDITORIA_FECHA
ON AUDITORIA(fecha);
GO

CREATE INDEX IX_MOVIMIENTO_PRODUCTO
ON MOVIMIENTO_INVENTARIO(id_producto);
GO

CREATE INDEX IX_ADECUACION_ESTUDIANTE
ON ADECUACION_ACADEMICA(id_estudiante);
GO

CREATE INDEX IX_ALERTA_ESTUDIANTE
ON ALERTA_ACADEMICA(id_estudiante);
GO


/* ============================================================
   ROLES INICIALES
   ============================================================ */

INSERT INTO ROL
(
    nombre,
    descripcion,
    estado
)
VALUES
(
    'Administrador',
    'Administra usuarios, configuración y funcionamiento general del sistema.',
    1
),
(
    'Director',
    'Supervisa procesos académicos y aprueba planes de estudio.',
    1
),
(
    'Docente',
    'Gestiona planes, asistencia, evaluaciones, notas y material didáctico.',
    1
),
(
    'Apoyo',
    'Registra y da seguimiento a las adecuaciones académicas.',
    1
),
(
    'Evaluación',
    'Consulta información académica y genera reportes y estadísticas.',
    1
);
GO


/* ============================================================
   CONSULTA DE VERIFICACIÓN
   ============================================================ */

SELECT
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO


USE AulaVirtualCopalchi;
GO

/* ============================================================
   TABLA: PERMISO
   Define las funcionalidades disponibles en el sistema
   ============================================================ */

IF OBJECT_ID('PERMISO', 'U') IS NULL
BEGIN
    CREATE TABLE PERMISO
    (
        id_permiso INT IDENTITY(1,1) NOT NULL,
        nombre NVARCHAR(100) NOT NULL,
        descripcion NVARCHAR(250) NULL,
        modulo NVARCHAR(100) NOT NULL,
        estado BIT NOT NULL
            CONSTRAINT DF_PERMISO_ESTADO DEFAULT 1,

        CONSTRAINT PK_PERMISO
            PRIMARY KEY (id_permiso),

        CONSTRAINT UQ_PERMISO_NOMBRE
            UNIQUE (nombre)
    );
END
GO


/* ============================================================
   TABLA: ROL_PERMISO
   Relación muchos a muchos entre roles y permisos
   ============================================================ */

IF OBJECT_ID('ROL_PERMISO', 'U') IS NULL
BEGIN
    CREATE TABLE ROL_PERMISO
    (
        id_rol INT NOT NULL,
        id_permiso INT NOT NULL,

        CONSTRAINT PK_ROL_PERMISO
            PRIMARY KEY (id_rol, id_permiso),

        CONSTRAINT FK_ROL_PERMISO_ROL
            FOREIGN KEY (id_rol)
            REFERENCES ROL(id_rol),

        CONSTRAINT FK_ROL_PERMISO_PERMISO
            FOREIGN KEY (id_permiso)
            REFERENCES PERMISO(id_permiso)
    );
END


GO


SELECT * FROM ROL;

INSERT INTO PERMISO (nombre, descripcion, modulo)
VALUES
('GESTIONAR_ROLES',
 'Crear, editar, eliminar y asignar roles',
 'Roles'),

('GESTIONAR_USUARIOS',
 'Administrar usuarios del sistema',
 'Usuarios'),

('GESTIONAR_ESTUDIANTES',
 'Administrar estudiantes',
 'Estudiantes'),

('GESTIONAR_PLANES',
 'Administrar planes de estudio',
 'Planes de Estudio'),

('GESTIONAR_ASISTENCIA',
 'Registrar y consultar asistencia',
 'Asistencia'),

('GESTIONAR_NOTAS',
 'Registrar y consultar calificaciones',
 'Notas'),

('GESTIONAR_MATERIAL',
 'Administrar material didáctico',
 'Material Didáctico'),

('VER_REPORTES',
 'Consultar y generar reportes',
 'Reportes'),

('VER_AUDITORIA',
 'Consultar registros de auditoría',
 'Auditoría'),

('GESTIONAR_INVENTARIO',
 'Administrar inventarios',
 'Inventario');
GO


USE AulaVirtualCopalchi;
GO

SELECT * FROM PERMISO;
SELECT * FROM ROL_PERMISO;



/* =====================================================================
   PREREQUISITO: tablas PERMISO y ROL_PERMISO
   Las usan Roles, Gestión Académica y Gestión de Notas. Si ya existen en
   tu base no hace nada (se puede ejecutar las veces que se quiera).
   Columnas tomadas de lo que usa RolesController (id_permiso, nombre,
   descripcion, modulo, estado / id_rol, id_permiso).
   ===================================================================== */

USE AulaVirtualCopalchi;
GO

IF OBJECT_ID(N'dbo.PERMISO', N'U') IS NULL
    CREATE TABLE dbo.PERMISO
    (
        id_permiso  INT IDENTITY(1,1) CONSTRAINT PK_PERMISO PRIMARY KEY,
        nombre      NVARCHAR(100) NOT NULL CONSTRAINT UQ_PERMISO_NOMBRE UNIQUE,
        descripcion NVARCHAR(250) NULL,
        modulo      NVARCHAR(100) NOT NULL,
        estado      BIT NOT NULL CONSTRAINT DF_PERMISO_ESTADO DEFAULT 1
    );
GO

IF OBJECT_ID(N'dbo.ROL_PERMISO', N'U') IS NULL
    CREATE TABLE dbo.ROL_PERMISO
    (
        id_rol     INT NOT NULL CONSTRAINT FK_ROLPERMISO_ROL     REFERENCES dbo.ROL(id_rol),
        id_permiso INT NOT NULL CONSTRAINT FK_ROLPERMISO_PERMISO REFERENCES dbo.PERMISO(id_permiso),
        CONSTRAINT PK_ROL_PERMISO PRIMARY KEY (id_rol, id_permiso)
    );
GO

SELECT name AS tabla_existente FROM sys.tables WHERE name IN (N'PERMISO', N'ROL_PERMISO');
GO


/* =====================================================================
   MÓDULO: GESTIÓN DE NOTAS
   Base de datos: AulaVirtualCopalchi
   ---------------------------------------------------------------------
   Se ejecuta DESPUÉS de los scripts de Gestión Académica (usa PERMISO,
   ROL_PERMISO, AUDITORIA, GRUPO, MATRICULA, ESTUDIANTE, DOCENTE).
   Se puede ejecutar completo las veces que se necesite.

   Reglas que se validan aquí (así valen igual al registrar a mano o al
   importar desde Excel):
     - Solo el docente del grupo registra o modifica notas y evaluaciones.
     - La nota va de 0 a la nota máxima de la evaluación.
     - El estudiante debe tener matrícula activa en el grupo de la evaluación.
     - Los porcentajes de una asignatura en un período no pasan de 100 %.
   Convención: @id_docente = 0 significa "sin restricción de docente".
   ===================================================================== */

USE AulaVirtualCopalchi;
GO

/* =====================================================================
   1. CAMBIOS DE ESTRUCTURA
   ===================================================================== */

-- Las vistas filtran por asignatura y trimestre, y la BD no los tenía.
-- Las evaluaciones que ya existan quedan como asignatura "General" y trimestre 1.
IF COL_LENGTH(N'dbo.EVALUACION', N'asignatura') IS NULL
    ALTER TABLE dbo.EVALUACION ADD asignatura NVARCHAR(100) NOT NULL
        CONSTRAINT DF_EVALUACION_ASIGNATURA DEFAULT N'General';
GO

IF COL_LENGTH(N'dbo.EVALUACION', N'trimestre') IS NULL
    ALTER TABLE dbo.EVALUACION ADD trimestre TINYINT NOT NULL
        CONSTRAINT DF_EVALUACION_TRIMESTRE DEFAULT 1
        CONSTRAINT CK_EVALUACION_TRIMESTRE CHECK (trimestre BETWEEN 1 AND 3);
GO

-- Notas de estudiantes que vienen de otra escuela (no pertenecen a ningún grupo
-- ni evaluación de esta escuela, por eso no afectan promedios de los grupos).
IF OBJECT_ID(N'dbo.CALIFICACION_EXTERNA', N'U') IS NULL
    CREATE TABLE dbo.CALIFICACION_EXTERNA
    (   id_calificacion_externa INT IDENTITY(1,1) CONSTRAINT PK_CALIFICACION_EXTERNA PRIMARY KEY,
        id_estudiante           INT NOT NULL
            CONSTRAINT FK_CALEXT_ESTUDIANTE REFERENCES dbo.ESTUDIANTE(id_estudiante),
        escuela_procedencia     NVARCHAR(150) NOT NULL,
        asignatura              NVARCHAR(100) NOT NULL,
        periodo                 NVARCHAR(50)  NOT NULL,
        anio                    INT NOT NULL CONSTRAINT CK_CALEXT_ANIO CHECK (anio >= 2000),
        nota                    DECIMAL(6,2) NOT NULL CONSTRAINT CK_CALEXT_NOTA CHECK (nota BETWEEN 0 AND 100),
        id_usuario_registro     INT NULL
            CONSTRAINT FK_CALEXT_USUARIO REFERENCES dbo.USUARIO(id_usuario),
        fecha_registro          DATETIME2(0) NOT NULL CONSTRAINT DF_CALEXT_FECHA DEFAULT SYSDATETIME(),
        CONSTRAINT UQ_CALEXT UNIQUE (id_estudiante, escuela_procedencia, asignatura, anio, periodo));
GO

/* =====================================================================
   2. PERMISOS DEL MÓDULO (se ajustan luego en Roles → Permisos)
   ===================================================================== */

INSERT INTO dbo.PERMISO (nombre, descripcion, modulo)
SELECT v.nombre, v.descripcion, v.modulo
FROM (VALUES
        (N'REGISTRAR_NOTAS', N'Registrar, importar, modificar y consultar notas de sus grupos (docentes)', N'Gestión de Notas'),
        (N'VER_TODAS_NOTAS', N'Consultar las notas de todos los grupos (solo lectura)',                    N'Gestión de Notas')
     ) v (nombre, descripcion, modulo)
WHERE NOT EXISTS (SELECT 1 FROM dbo.PERMISO p WHERE p.nombre = v.nombre);

INSERT INTO dbo.ROL_PERMISO (id_rol, id_permiso)
SELECT r.id_rol, p.id_permiso
FROM (VALUES
        (N'Docente',       N'REGISTRAR_NOTAS'),
        (N'Director',      N'VER_TODAS_NOTAS'),
        (N'Administrador', N'VER_TODAS_NOTAS')
     ) v (rol, permiso)
JOIN dbo.ROL r     ON r.nombre = v.rol
JOIN dbo.PERMISO p ON p.nombre = v.permiso
WHERE NOT EXISTS (SELECT 1 FROM dbo.ROL_PERMISO x WHERE x.id_rol = r.id_rol AND x.id_permiso = p.id_permiso);
GO

/* =====================================================================
   3. GRUPOS Y EVALUACIONES
   ===================================================================== */

-- Pantalla Index. @id_docente = 0 -> todos los grupos (director). @anio = 0 -> todos.
-- Pendientes = total_estudiantes * total_evaluaciones - notas_registradas (se calcula en C#).
CREATE OR ALTER PROCEDURE dbo.SP_GN_Grupo_Resumen
    @id_docente INT = 0, @anio INT = 0
AS
    SELECT g.id_grupo, g.nombre AS grupo, g.nivel, g.anio,
           LTRIM(RTRIM(CONCAT(u.nombre, N' ', u.apellido1, N' ', u.apellido2))) AS docente,
           (SELECT COUNT(*) FROM dbo.MATRICULA m
             WHERE m.id_grupo = g.id_grupo AND m.estado = N'Activa') AS total_estudiantes,
           (SELECT COUNT(*) FROM dbo.EVALUACION ev
             WHERE ev.id_grupo = g.id_grupo AND ev.estado = 1) AS total_evaluaciones,
           (SELECT COUNT(*)
              FROM dbo.CALIFICACION c
              JOIN dbo.EVALUACION ev ON ev.id_evaluacion = c.id_evaluacion AND ev.estado = 1
              JOIN dbo.MATRICULA m   ON m.id_estudiante = c.id_estudiante
                                    AND m.id_grupo = ev.id_grupo AND m.estado = N'Activa'
             WHERE ev.id_grupo = g.id_grupo) AS notas_registradas
    FROM dbo.GRUPO g
    LEFT JOIN dbo.DOCENTE d ON d.id_docente = g.id_docente
    LEFT JOIN dbo.USUARIO u ON u.id_usuario = d.id_usuario
    WHERE g.estado = 1
      AND (@id_docente = 0 OR g.id_docente = @id_docente)
      AND (@anio = 0 OR g.anio = @anio)
    ORDER BY g.anio DESC, g.nombre;
GO

-- Asignaturas que ya tienen evaluaciones en el grupo (para el filtro).
CREATE OR ALTER PROCEDURE dbo.SP_GN_Asignatura_Listar
    @id_grupo INT
AS
    SELECT DISTINCT asignatura
    FROM dbo.EVALUACION
    WHERE id_grupo = @id_grupo AND estado = 1
    ORDER BY asignatura;
GO

-- @asignatura = '' y @trimestre = 0 -> sin filtro.
CREATE OR ALTER PROCEDURE dbo.SP_GN_Evaluacion_Listar
    @id_grupo INT, @asignatura NVARCHAR(100) = N'', @trimestre TINYINT = 0
AS
    SELECT ev.id_evaluacion, ev.id_grupo, ev.asignatura, ev.trimestre, ev.nombre,
           ev.porcentaje, ev.fecha, ev.nota_maxima,
           SUM(ev.porcentaje) OVER (PARTITION BY ev.asignatura, ev.trimestre) AS porcentaje_total
    FROM dbo.EVALUACION ev
    WHERE ev.id_grupo = @id_grupo AND ev.estado = 1
      AND (ISNULL(@asignatura, N'') = N'' OR ev.asignatura = @asignatura)
      AND (@trimestre = 0 OR ev.trimestre = @trimestre)
    ORDER BY ev.trimestre, ev.asignatura, ev.fecha, ev.id_evaluacion;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GN_Evaluacion_Insertar
    @id_grupo INT, @asignatura NVARCHAR(100), @trimestre TINYINT, @nombre NVARCHAR(150),
    @porcentaje DECIMAL(5,2), @fecha DATE = NULL, @nota_maxima DECIMAL(6,2) = 100,
    @id_usuario INT, @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.GRUPO WHERE id_grupo = @id_grupo AND estado = 1)
        THROW 50020, N'El grupo no existe o está inactivo.', 1;
    IF @id_docente > 0
       AND NOT EXISTS (SELECT 1 FROM dbo.GRUPO WHERE id_grupo = @id_grupo AND id_docente = @id_docente)
        THROW 50011, N'Solo el docente del grupo puede crear evaluaciones.', 1;
    IF @porcentaje <= 0 OR @porcentaje > 100
        THROW 50023, N'El porcentaje debe ser mayor que 0 y no puede superar 100.', 1;
    IF @nota_maxima <= 0
        THROW 50024, N'La nota máxima debe ser mayor que 0.', 1;
    IF EXISTS (SELECT 1 FROM dbo.EVALUACION
               WHERE id_grupo = @id_grupo AND asignatura = @asignatura AND trimestre = @trimestre
                 AND estado = 1 AND nombre = @nombre)
        THROW 50021, N'Ya existe una evaluación con ese nombre en la asignatura y el período.', 1;
    DECLARE @suma_actual DECIMAL(6,2) =
        (SELECT ISNULL(SUM(porcentaje), 0) FROM dbo.EVALUACION
         WHERE id_grupo = @id_grupo AND asignatura = @asignatura AND trimestre = @trimestre
           AND estado = 1);
    IF @suma_actual + @porcentaje > 100
        THROW 50022, N'La suma de los porcentajes de la asignatura en este período no puede superar 100 %.', 1;
    INSERT INTO dbo.EVALUACION (id_grupo, asignatura, trimestre, nombre, porcentaje, fecha, nota_maxima)
    VALUES (@id_grupo, @asignatura, @trimestre, @nombre, @porcentaje, @fecha, @nota_maxima);
    DECLARE @id_nueva INT = SCOPE_IDENTITY();
    INSERT INTO dbo.AUDITORIA (id_usuario, accion, modulo, tabla_afectada, registro_afectado, descripcion)
    VALUES (@id_usuario, N'Crear evaluación', N'Gestión de notas', N'EVALUACION',
            CAST(@id_nueva AS NVARCHAR(100)),
            CONCAT(@asignatura, N' - ', @nombre, N' (', @porcentaje, N' %)'));
END
GO

/* =====================================================================
   4. CALIFICACIONES
   ===================================================================== */

-- Pantalla Registrar: una fila por estudiante y evaluación (nota NULL si aún no tiene).
-- Si el grupo no tiene evaluaciones para ese filtro, los estudiantes salen con evaluación NULL.
CREATE OR ALTER PROCEDURE dbo.SP_GN_Calificacion_Matriz
    @id_grupo INT, @asignatura NVARCHAR(100), @trimestre TINYINT
AS
    SELECT e.id_estudiante, e.identificacion,
           LTRIM(RTRIM(CONCAT(e.nombre, N' ', e.apellido1, N' ', e.apellido2))) AS nombre_completo,
           ev.id_evaluacion, ev.nombre AS evaluacion, ev.porcentaje, ev.nota_maxima,
           c.id_calificacion, c.nota
    FROM dbo.MATRICULA m
    JOIN dbo.ESTUDIANTE e ON e.id_estudiante = m.id_estudiante
    LEFT JOIN dbo.EVALUACION ev ON ev.id_grupo = m.id_grupo AND ev.estado = 1
                               AND ev.asignatura = @asignatura AND ev.trimestre = @trimestre
    LEFT JOIN dbo.CALIFICACION c ON c.id_evaluacion = ev.id_evaluacion
                                AND c.id_estudiante = e.id_estudiante
    WHERE m.id_grupo = @id_grupo AND m.estado = N'Activa'
    ORDER BY e.apellido1, e.apellido2, e.nombre, ev.fecha, ev.id_evaluacion;
GO

-- Pantalla Editar: una nota con los datos para mostrarla y para saber de qué docente es.
CREATE OR ALTER PROCEDURE dbo.SP_GN_Calificacion_Obtener
    @id_calificacion INT
AS
    SELECT c.id_calificacion, c.id_evaluacion, c.id_estudiante, c.nota, c.observacion, c.fecha_registro,
           ev.nombre AS evaluacion, ev.asignatura, ev.trimestre, ev.porcentaje, ev.nota_maxima,
           g.id_grupo, g.nombre AS grupo, g.id_docente,
           LTRIM(RTRIM(CONCAT(e.nombre, N' ', e.apellido1, N' ', e.apellido2))) AS estudiante
    FROM dbo.CALIFICACION c
    JOIN dbo.EVALUACION ev ON ev.id_evaluacion = c.id_evaluacion
    JOIN dbo.GRUPO g       ON g.id_grupo = ev.id_grupo
    JOIN dbo.ESTUDIANTE e  ON e.id_estudiante = c.id_estudiante
    WHERE c.id_calificacion = @id_calificacion;
GO

-- Registrar, editar e importar usan este mismo procedimiento. Recibe un arreglo JSON:
--   [{"id_evaluacion":1,"id_estudiante":5,"nota":85.5,"observacion":"..."}, ...]
-- Todo o nada: si una fila no cumple las reglas no se guarda ninguna. Las notas vacías
-- (null) se ignoran. Registra en AUDITORIA solo los cambios reales (nota anterior -> nueva).
CREATE OR ALTER PROCEDURE dbo.SP_GN_Calificacion_GuardarLote
    @id_usuario INT, @id_docente INT = 0, @json NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @lote TABLE
    (   id_evaluacion INT NOT NULL,
        id_estudiante INT NOT NULL,
        nota          DECIMAL(6,2) NULL,
        observacion   NVARCHAR(500) NULL,
        PRIMARY KEY (id_evaluacion, id_estudiante));
    INSERT INTO @lote (id_evaluacion, id_estudiante, nota, observacion)
    SELECT id_evaluacion, id_estudiante, nota, observacion
    FROM OPENJSON(@json)
    WITH (id_evaluacion INT           '$.id_evaluacion',
          id_estudiante INT           '$.id_estudiante',
          nota          DECIMAL(6,2)  '$.nota',
          observacion   NVARCHAR(500) '$.observacion');
    DELETE FROM @lote WHERE nota IS NULL;
    IF NOT EXISTS (SELECT 1 FROM @lote)
        THROW 50010, N'No hay notas para guardar.', 1;
    IF EXISTS (SELECT 1 FROM @lote l
               LEFT JOIN dbo.EVALUACION ev ON ev.id_evaluacion = l.id_evaluacion
               WHERE ev.id_evaluacion IS NULL OR ev.estado = 0)
        THROW 50014, N'Hay evaluaciones que no existen o están inactivas.', 1;
    IF @id_docente > 0
       AND EXISTS (SELECT 1 FROM @lote l
                   JOIN dbo.EVALUACION ev ON ev.id_evaluacion = l.id_evaluacion
                   JOIN dbo.GRUPO g       ON g.id_grupo = ev.id_grupo
                   WHERE ISNULL(g.id_docente, 0) <> @id_docente)
        THROW 50011, N'Solo el docente del grupo puede registrar o modificar sus calificaciones.', 1;
    IF EXISTS (SELECT 1 FROM @lote l
               JOIN dbo.EVALUACION ev ON ev.id_evaluacion = l.id_evaluacion
               WHERE NOT EXISTS (SELECT 1 FROM dbo.MATRICULA m
                                 WHERE m.id_estudiante = l.id_estudiante
                                   AND m.id_grupo = ev.id_grupo AND m.estado = N'Activa'))
        THROW 50012, N'Hay estudiantes que no pertenecen al grupo de la evaluación.', 1;
    IF EXISTS (SELECT 1 FROM @lote l
               JOIN dbo.EVALUACION ev ON ev.id_evaluacion = l.id_evaluacion
               WHERE l.nota < 0 OR l.nota > ev.nota_maxima)
        THROW 50013, N'Hay notas fuera del rango permitido (de 0 a la nota máxima de la evaluación).', 1;
    BEGIN TRANSACTION;

    -- Bitácora: solo notas nuevas o que cambian
    INSERT INTO dbo.AUDITORIA (id_usuario, accion, modulo, tabla_afectada, registro_afectado, descripcion)
    SELECT @id_usuario,
           CASE WHEN c.id_calificacion IS NULL THEN N'Registrar nota' ELSE N'Modificar nota' END,
           N'Gestión de notas', N'CALIFICACION',
           CONCAT(l.id_evaluacion, N'/', l.id_estudiante),
           CASE WHEN c.id_calificacion IS NULL
                THEN CONCAT(N'Nota ', l.nota)
                ELSE CONCAT(N'Nota ', c.nota, N' -> ', l.nota) END
    FROM @lote l
    LEFT JOIN dbo.CALIFICACION c ON c.id_evaluacion = l.id_evaluacion AND c.id_estudiante = l.id_estudiante
    WHERE c.id_calificacion IS NULL OR c.nota <> l.nota;
    UPDATE c
    SET c.nota = l.nota,
        c.observacion = ISNULL(l.observacion, c.observacion),
        c.fecha_registro = SYSDATETIME()
    FROM dbo.CALIFICACION c
    JOIN @lote l ON l.id_evaluacion = c.id_evaluacion AND l.id_estudiante = c.id_estudiante
    WHERE c.nota <> l.nota OR (l.observacion IS NOT NULL AND ISNULL(c.observacion, N'') <> l.observacion);
    INSERT INTO dbo.CALIFICACION (id_evaluacion, id_estudiante, nota, observacion)
    SELECT l.id_evaluacion, l.id_estudiante, l.nota, l.observacion
    FROM @lote l
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CALIFICACION c
                      WHERE c.id_evaluacion = l.id_evaluacion AND c.id_estudiante = l.id_estudiante);
    COMMIT TRANSACTION;
END
GO

/* =====================================================================
   5. NOTAS DE ESCUELA ANTERIOR (importación desde Excel)
   ===================================================================== */

-- Arreglo JSON:
--   [{"id_estudiante":5,"escuela_procedencia":"Escuela X","asignatura":"Matemática",
--     "periodo":"I Trimestre","anio":2025,"nota":88}, ...]
-- Si ya existe la misma nota (estudiante, escuela, asignatura, año y período) se actualiza.
CREATE OR ALTER PROCEDURE dbo.SP_GN_CalificacionExterna_GuardarLote
    @id_usuario INT, @id_docente INT = 0, @json NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @lote TABLE
    (   id_estudiante       INT NOT NULL,
        escuela_procedencia NVARCHAR(150) NOT NULL,
        asignatura          NVARCHAR(100) NOT NULL,
        periodo             NVARCHAR(50)  NOT NULL,
        anio                INT NOT NULL,
        nota                DECIMAL(6,2) NOT NULL,
        PRIMARY KEY (id_estudiante, escuela_procedencia, asignatura, anio, periodo));
    INSERT INTO @lote (id_estudiante, escuela_procedencia, asignatura, periodo, anio, nota)
    SELECT id_estudiante, escuela_procedencia, asignatura, periodo, anio, nota
    FROM OPENJSON(@json)
    WITH (id_estudiante       INT           '$.id_estudiante',
          escuela_procedencia NVARCHAR(150) '$.escuela_procedencia',
          asignatura          NVARCHAR(100) '$.asignatura',
          periodo             NVARCHAR(50)  '$.periodo',
          anio                INT           '$.anio',
          nota                DECIMAL(6,2)  '$.nota');
    IF NOT EXISTS (SELECT 1 FROM @lote)
        THROW 50010, N'No hay notas para guardar.', 1;
    IF EXISTS (SELECT 1 FROM @lote l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.ESTUDIANTE e
                                 WHERE e.id_estudiante = l.id_estudiante AND e.estado = 1))
        THROW 50015, N'Hay estudiantes que no existen o están inactivos.', 1;
    IF @id_docente > 0
       AND EXISTS (SELECT 1 FROM @lote l
                   WHERE NOT EXISTS (SELECT 1 FROM dbo.MATRICULA m
                                     JOIN dbo.GRUPO g ON g.id_grupo = m.id_grupo
                                     WHERE m.id_estudiante = l.id_estudiante
                                       AND m.estado = N'Activa' AND g.id_docente = @id_docente))
        THROW 50011, N'Solo puede registrar notas anteriores de estudiantes de sus grupos.', 1;
    IF EXISTS (SELECT 1 FROM @lote WHERE nota < 0 OR nota > 100)
        THROW 50013, N'Hay notas fuera del rango permitido (de 0 a 100).', 1;
    BEGIN TRANSACTION;
    UPDATE x
    SET x.nota = l.nota, x.id_usuario_registro = @id_usuario, x.fecha_registro = SYSDATETIME()
    FROM dbo.CALIFICACION_EXTERNA x
    JOIN @lote l ON l.id_estudiante = x.id_estudiante AND l.escuela_procedencia = x.escuela_procedencia
                AND l.asignatura = x.asignatura AND l.anio = x.anio AND l.periodo = x.periodo;
    INSERT INTO dbo.CALIFICACION_EXTERNA
        (id_estudiante, escuela_procedencia, asignatura, periodo, anio, nota, id_usuario_registro)
    SELECT l.id_estudiante, l.escuela_procedencia, l.asignatura, l.periodo, l.anio, l.nota, @id_usuario
    FROM @lote l
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CALIFICACION_EXTERNA x
                      WHERE x.id_estudiante = l.id_estudiante AND x.escuela_procedencia = l.escuela_procedencia
                        AND x.asignatura = l.asignatura AND x.anio = l.anio AND x.periodo = l.periodo);
    INSERT INTO dbo.AUDITORIA (id_usuario, accion, modulo, tabla_afectada, descripcion)
    VALUES (@id_usuario, N'Importar notas de escuela anterior', N'Gestión de notas',
            N'CALIFICACION_EXTERNA',
            CONCAT(N'Se guardaron ', (SELECT COUNT(*) FROM @lote), N' notas de escuelas anteriores.'));
    COMMIT TRANSACTION;
END
GO

/* =====================================================================
   6. HISTORIAL
   ===================================================================== */

-- Notas de esta escuela y, para los mismos estudiantes, las de escuelas anteriores.
-- @id_docente = 0 -> todos los grupos (director). Los demás filtros: 0 / '' = sin filtro.
-- Las notas de escuela anterior no tienen trimestre propio: con @trimestre > 0 no se incluyen.
CREATE OR ALTER PROCEDURE dbo.SP_GN_Calificacion_Historial
    @id_docente INT = 0, @id_grupo INT = 0, @id_estudiante INT = 0,
    @asignatura NVARCHAR(100) = N'', @trimestre TINYINT = 0
AS
    SELECT TOP (1000) h.*
    FROM (
        SELECT N'Escuela' AS origen, c.id_calificacion, c.id_estudiante,
               LTRIM(RTRIM(CONCAT(e.nombre, N' ', e.apellido1, N' ', e.apellido2))) AS estudiante,
               g.nombre AS grupo, ev.asignatura,
               CASE ev.trimestre WHEN 1 THEN N'I Trimestre' WHEN 2 THEN N'II Trimestre'
                                 ELSE N'III Trimestre' END AS periodo,
               g.anio, ev.nombre AS evaluacion, c.nota, ev.nota_maxima,
               CAST(NULL AS NVARCHAR(150)) AS escuela_procedencia,
               c.fecha_registro AS fecha, g.id_docente
        FROM dbo.CALIFICACION c
        JOIN dbo.EVALUACION ev ON ev.id_evaluacion = c.id_evaluacion AND ev.estado = 1
        JOIN dbo.GRUPO g       ON g.id_grupo = ev.id_grupo
        JOIN dbo.ESTUDIANTE e  ON e.id_estudiante = c.id_estudiante
        WHERE (@id_docente = 0 OR g.id_docente = @id_docente)
          AND (@id_grupo = 0 OR g.id_grupo = @id_grupo)
          AND (@id_estudiante = 0 OR c.id_estudiante = @id_estudiante)
          AND (ISNULL(@asignatura, N'') = N'' OR ev.asignatura = @asignatura)
          AND (@trimestre = 0 OR ev.trimestre = @trimestre)
        UNION ALL
        SELECT N'Escuela anterior', CAST(NULL AS INT), x.id_estudiante,
               LTRIM(RTRIM(CONCAT(e.nombre, N' ', e.apellido1, N' ', e.apellido2))),
               CAST(NULL AS NVARCHAR(100)), x.asignatura, x.periodo, x.anio,
               N'Nota de escuela anterior', x.nota, CAST(100 AS DECIMAL(6,2)),
               x.escuela_procedencia, x.fecha_registro, CAST(NULL AS INT)
        FROM dbo.CALIFICACION_EXTERNA x
        JOIN dbo.ESTUDIANTE e ON e.id_estudiante = x.id_estudiante
        WHERE @trimestre = 0
          AND (@id_estudiante = 0 OR x.id_estudiante = @id_estudiante)
          AND (ISNULL(@asignatura, N'') = N'' OR x.asignatura = @asignatura)
          AND ((@id_docente = 0 AND @id_grupo = 0)
               OR EXISTS (SELECT 1 FROM dbo.MATRICULA m
                          JOIN dbo.GRUPO g ON g.id_grupo = m.id_grupo
                          WHERE m.id_estudiante = x.id_estudiante AND m.estado = N'Activa'
                            AND (@id_docente = 0 OR g.id_docente = @id_docente)
                            AND (@id_grupo = 0 OR g.id_grupo = @id_grupo)))
    ) h
    ORDER BY h.fecha DESC, h.estudiante;
GO

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

/* =====================================================================
   VERIFICACIÓN: permisos de Gestión de Notas por rol
   ===================================================================== */

SELECT r.nombre AS rol, p.nombre AS permiso
FROM dbo.ROL_PERMISO rp
JOIN dbo.ROL r     ON r.id_rol = rp.id_rol
JOIN dbo.PERMISO p ON p.id_permiso = rp.id_permiso
WHERE p.nombre IN (N'REGISTRAR_NOTAS', N'VER_TODAS_NOTAS')
ORDER BY r.nombre, p.nombre;
GO
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




/* Gestión de notas - modificar y eliminar evaluaciones */
USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GN_Evaluacion_Actualizar
    @id_evaluacion INT, @nombre NVARCHAR(150), @porcentaje DECIMAL(5,2),
    @fecha DATE = NULL, @nota_maxima DECIMAL(6,2) = 100,
    @id_usuario INT, @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @id_grupo INT, @asignatura NVARCHAR(100), @trimestre TINYINT,
            @nombre_ant NVARCHAR(150), @pct_ant DECIMAL(5,2);
    SELECT @id_grupo = id_grupo, @asignatura = asignatura, @trimestre = trimestre,
           @nombre_ant = nombre, @pct_ant = porcentaje
    FROM dbo.EVALUACION
    WHERE id_evaluacion = @id_evaluacion AND estado = 1;
    IF @id_grupo IS NULL
        THROW 50025, N'La evaluación no existe.', 1;
    IF @id_docente > 0
       AND NOT EXISTS (SELECT 1 FROM dbo.GRUPO WHERE id_grupo = @id_grupo AND id_docente = @id_docente)
        THROW 50011, N'Solo el docente del grupo puede modificar evaluaciones.', 1;
    IF @porcentaje <= 0 OR @porcentaje > 100
        THROW 50023, N'El porcentaje debe ser mayor que 0 y no puede superar 100.', 1;
    IF @nota_maxima <= 0
        THROW 50024, N'La nota máxima debe ser mayor que 0.', 1;
    IF EXISTS (SELECT 1 FROM dbo.EVALUACION
               WHERE id_grupo = @id_grupo AND asignatura = @asignatura AND trimestre = @trimestre
                 AND estado = 1 AND nombre = @nombre AND id_evaluacion <> @id_evaluacion)
        THROW 50021, N'Ya existe una evaluación con ese nombre en la asignatura y el período.', 1;
    IF EXISTS (SELECT 1 FROM dbo.CALIFICACION WHERE id_evaluacion = @id_evaluacion AND nota > @nota_maxima)
        THROW 50026, N'Hay notas registradas mayores que la nueva nota máxima.', 1;
    DECLARE @suma_otras DECIMAL(6,2) =
        (SELECT ISNULL(SUM(porcentaje), 0) FROM dbo.EVALUACION
         WHERE id_grupo = @id_grupo AND asignatura = @asignatura AND trimestre = @trimestre
           AND estado = 1 AND id_evaluacion <> @id_evaluacion);
    IF @suma_otras + @porcentaje > 100
        THROW 50022, N'La suma de los porcentajes de la asignatura en este período no puede superar 100 %.', 1;
    UPDATE dbo.EVALUACION
    SET nombre = @nombre, porcentaje = @porcentaje, fecha = @fecha, nota_maxima = @nota_maxima
    WHERE id_evaluacion = @id_evaluacion;
    INSERT INTO dbo.AUDITORIA (id_usuario, accion, modulo, tabla_afectada, registro_afectado, descripcion)
    VALUES (@id_usuario, N'Modificar evaluación', N'Gestión de notas', N'EVALUACION',
            CAST(@id_evaluacion AS NVARCHAR(100)),
            CONCAT(@asignatura, N' - ', @nombre_ant, N' -> ', @nombre,
                   N' (', @pct_ant, N' % -> ', @porcentaje, N' %)'));
END
GO

-- Eliminación lógica: la evaluación y sus notas dejan de mostrarse y de contar en las notas finales.
CREATE OR ALTER PROCEDURE dbo.SP_GN_Evaluacion_Eliminar
    @id_evaluacion INT, @id_usuario INT, @id_docente INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @id_grupo INT, @asignatura NVARCHAR(100), @nombre NVARCHAR(150);
    SELECT @id_grupo = id_grupo, @asignatura = asignatura, @nombre = nombre
    FROM dbo.EVALUACION
    WHERE id_evaluacion = @id_evaluacion AND estado = 1;
    IF @id_grupo IS NULL
        THROW 50025, N'La evaluación no existe.', 1;
    IF @id_docente > 0
       AND NOT EXISTS (SELECT 1 FROM dbo.GRUPO WHERE id_grupo = @id_grupo AND id_docente = @id_docente)
        THROW 50011, N'Solo el docente del grupo puede eliminar evaluaciones.', 1;
    UPDATE dbo.EVALUACION SET estado = 0 WHERE id_evaluacion = @id_evaluacion;
    INSERT INTO dbo.AUDITORIA (id_usuario, accion, modulo, tabla_afectada, registro_afectado, descripcion)
    VALUES (@id_usuario, N'Eliminar evaluación', N'Gestión de notas', N'EVALUACION',
            CAST(@id_evaluacion AS NVARCHAR(100)), CONCAT(@asignatura, N' - ', @nombre));
END
GO



/* Gestión de notas - datos para exportar a Excel (grupo completo o un estudiante) */
USE AulaVirtualCopalchi;
GO

CREATE OR ALTER PROCEDURE dbo.SP_GN_Reporte_Notas
    @id_docente INT = 0,        -- 0 = sin restricción (Director / Evaluación)
    @id_grupo INT,
    @id_estudiante INT = 0      -- 0 = todos los estudiantes del grupo
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.id_estudiante, e.identificacion,
           LTRIM(RTRIM(CONCAT(e.nombre, N' ', e.apellido1, N' ', e.apellido2))) AS estudiante,
           g.nombre AS grupo, g.anio,
           LTRIM(RTRIM(CONCAT(u.nombre, N' ', u.apellido1, N' ', u.apellido2))) AS docente,
           ev.asignatura, ev.trimestre, ev.nombre AS evaluacion, ev.porcentaje, ev.nota_maxima,
           c.nota
    FROM dbo.MATRICULA m
    JOIN dbo.ESTUDIANTE e        ON e.id_estudiante = m.id_estudiante
    JOIN dbo.GRUPO g             ON g.id_grupo = m.id_grupo
    LEFT JOIN dbo.DOCENTE d      ON d.id_docente = g.id_docente
    LEFT JOIN dbo.USUARIO u      ON u.id_usuario = d.id_usuario
    LEFT JOIN dbo.EVALUACION ev  ON ev.id_grupo = m.id_grupo AND ev.estado = 1
    LEFT JOIN dbo.CALIFICACION c ON c.id_evaluacion = ev.id_evaluacion
                                AND c.id_estudiante = e.id_estudiante
    WHERE m.id_grupo = @id_grupo AND m.estado = N'Activa'
      AND (@id_estudiante = 0 OR e.id_estudiante = @id_estudiante)
      AND (@id_docente = 0 OR g.id_docente = @id_docente)
    ORDER BY e.apellido1, e.apellido2, e.nombre, ev.asignatura, ev.trimestre, ev.fecha, ev.id_evaluacion;
END
GO



