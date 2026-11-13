-- Ejecutar en la base de datos del proyecto. No borra permisos ni asignaciones.
-- Permiso independiente de VER_REPORTES (Monitoreo académico).
IF NOT EXISTS (SELECT 1 FROM dbo.PERMISO WHERE nombre = N'VER_REPORTES_ACADEMICOS')
BEGIN
    INSERT INTO dbo.PERMISO(nombre, descripcion, modulo, estado)
    VALUES(N'VER_REPORTES_ACADEMICOS', N'Consultar reportes, estadísticas y exportar PDF y Excel', N'Reportes académicos', 1);
END
ELSE
BEGIN
    UPDATE dbo.PERMISO SET descripcion=N'Consultar reportes, estadísticas y exportar PDF y Excel',
        modulo=N'Reportes académicos', estado=1 WHERE nombre=N'VER_REPORTES_ACADEMICOS';
END;
-- Asignar desde Roles > Permisos a Docente, Director y Evaluación, según corresponda.
SELECT id_permiso, nombre, descripcion, modulo, estado FROM dbo.PERMISO WHERE nombre=N'VER_REPORTES_ACADEMICOS';
