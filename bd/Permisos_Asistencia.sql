USE AulaVirtualCopalchi;
GO
-- Registrar permisos sin duplicar y conservando los existentes.
-- Se requiere que dbo.PERMISO.id_permiso sea IDENTITY.
IF COLUMNPROPERTY(OBJECT_ID('dbo.PERMISO'), 'id_permiso', 'IsIdentity') <> 1
    THROW 50001, 'id_permiso no es IDENTITY. Revise la asignacion de identificadores antes de insertar.', 1;
GO
DECLARE @modulo NVARCHAR(150) = N'Asistencia';
SELECT TOP (1) @modulo = modulo
FROM dbo.PERMISO
WHERE nombre IN (N'REGISTRAR_ASISTENCIA', N'VER_TODA_ASISTENCIA')
ORDER BY CASE WHEN nombre = N'REGISTRAR_ASISTENCIA' THEN 0 ELSE 1 END;

DECLARE @nuevos TABLE (nombre NVARCHAR(150), descripcion NVARCHAR(250));
INSERT INTO @nuevos VALUES
(N'CONSULTAR_ASISTENCIA', N'Consultar asistencia de los grupos autorizados'),
(N'REGISTRAR_ASISTENCIA', N'Registrar asistencia'),
(N'EDITAR_ASISTENCIA', N'Editar registros de asistencia'),
(N'VER_HISTORIAL_ASISTENCIA', N'Consultar historial de asistencia'),
(N'VER_PORCENTAJE_ASISTENCIA', N'Consultar porcentajes de asistencia');

INSERT INTO dbo.PERMISO(nombre, descripcion, modulo, estado)
SELECT n.nombre, n.descripcion, @modulo, 1
FROM @nuevos n
WHERE NOT EXISTS (SELECT 1 FROM dbo.PERMISO p WHERE p.nombre = n.nombre);

SELECT id_permiso, nombre, descripcion, modulo, estado
FROM dbo.PERMISO WHERE modulo = @modulo ORDER BY nombre;
