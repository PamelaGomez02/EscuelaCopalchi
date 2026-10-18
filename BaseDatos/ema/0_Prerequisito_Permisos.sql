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
