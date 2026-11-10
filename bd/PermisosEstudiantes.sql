USE AulaVirtualCopalchi;
GO
-- Este script presupone que dbo.PERMISO.id_permiso es IDENTITY.
-- Verificar antes: SELECT COLUMNPROPERTY(OBJECT_ID('dbo.PERMISO'),'id_permiso','IsIdentity');
DECLARE @Modulo NVARCHAR(150);
SELECT TOP (1) @Modulo = modulo FROM dbo.PERMISO WHERE nombre LIKE '%ESTUDIANT%' ORDER BY id_permiso;
IF @Modulo IS NULL SET @Modulo = N'Estudiantes';
DECLARE @Nuevos TABLE(nombre NVARCHAR(150), descripcion NVARCHAR(250));
INSERT INTO @Nuevos VALUES
(N'VER_ESTUDIANTES',N'Consultar estudiantes'),
(N'CREAR_ESTUDIANTES',N'Registrar estudiantes'),
(N'EDITAR_ESTUDIANTES',N'Modificar datos de estudiantes'),
(N'IMPORTAR_ESTUDIANTES',N'Importar estudiantes desde Excel'),
(N'VER_EXPEDIENTE_ESTUDIANTES',N'Consultar expedientes'),
(N'EDITAR_EXPEDIENTE_ESTUDIANTES',N'Registrar y modificar adecuaciones, alergias y observaciones'),
(N'GESTIONAR_BAJAS_ESTUDIANTES',N'Dar de baja y consultar historial de bajas');
INSERT INTO dbo.PERMISO(nombre,descripcion,modulo,estado)
SELECT n.nombre,n.descripcion,@Modulo,1 FROM @Nuevos n
WHERE NOT EXISTS(SELECT 1 FROM dbo.PERMISO p WHERE p.nombre=n.nombre);
SELECT id_permiso,nombre,descripcion,modulo,estado FROM dbo.PERMISO WHERE nombre LIKE '%ESTUDIANT%' ORDER BY nombre;
GO
