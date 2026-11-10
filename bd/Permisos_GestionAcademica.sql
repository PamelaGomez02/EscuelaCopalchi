
USE AulaVirtualCopalchi;
GO

-- Ejecutar en la base AulaVirtualCopalchi. No asigna permisos a roles automáticamente.
IF NOT EXISTS (SELECT 1 FROM dbo.PERMISO WHERE nombre = N'VER_AULAS')
    INSERT INTO dbo.PERMISO (nombre, descripcion, modulo, estado) VALUES (N'VER_AULAS', N'Consultar aulas', N'Gestión Académica', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.PERMISO WHERE nombre = N'GESTIONAR_AULAS')
    INSERT INTO dbo.PERMISO (nombre, descripcion, modulo, estado) VALUES (N'GESTIONAR_AULAS', N'Crear y editar aulas', N'Gestión Académica', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.PERMISO WHERE nombre = N'VER_GRUPOS')
    INSERT INTO dbo.PERMISO (nombre, descripcion, modulo, estado) VALUES (N'VER_GRUPOS', N'Consultar grupos, horarios y docentes', N'Gestión Académica', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.PERMISO WHERE nombre = N'GESTIONAR_GRUPOS')
    INSERT INTO dbo.PERMISO (nombre, descripcion, modulo, estado) VALUES (N'GESTIONAR_GRUPOS', N'Crear grupos, editar horarios y asignar docentes', N'Gestión Académica', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.PERMISO WHERE nombre = N'ASIGNAR_ESTUDIANTES_GRUPOS')
    INSERT INTO dbo.PERMISO (nombre, descripcion, modulo, estado) VALUES (N'ASIGNAR_ESTUDIANTES_GRUPOS', N'Asignar, trasladar y retirar estudiantes de grupos', N'Gestión Académica', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.PERMISO WHERE nombre = N'IMPORTAR_ASIGNACIONES')
    INSERT INTO dbo.PERMISO (nombre, descripcion, modulo, estado) VALUES (N'IMPORTAR_ASIGNACIONES', N'Importar grupos y docentes desde Excel', N'Gestión Académica', 1);

SELECT id_permiso, nombre, descripcion, modulo, estado FROM dbo.PERMISO WHERE modulo = N'Gestión Académica';
