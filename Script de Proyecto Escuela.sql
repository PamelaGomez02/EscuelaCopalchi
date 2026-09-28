
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