using EscuelaCopalchi.UI.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using ClosedXML.Excel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    [Authorize]
    public class InventarioLimpiezaController : Controller
    {
        private readonly string cadena = ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"].ConnectionString;
        private int? UsuarioActual()
        {
            if (!Request.IsAuthenticated) return null;
            int id;
            if (Session["IdUsuario"] != null && int.TryParse(Session["IdUsuario"].ToString(), out id) && id > 0)
            {
                using (var cn = new SqlConnection(cadena))
                using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.USUARIO WHERE id_usuario=@id AND estado=1", cn))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    cn.Open();
                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) return id;
                }
            }
            if (string.IsNullOrWhiteSpace(User.Identity.Name)) return null;
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand("SELECT id_usuario FROM dbo.USUARIO WHERE correo=@correo AND estado=1", cn))
            {
                cmd.Parameters.Add("@correo", SqlDbType.NVarChar, 255).Value = User.Identity.Name;
                cn.Open(); var o = cmd.ExecuteScalar();
                if (o == null || o == DBNull.Value) return null;
                id = Convert.ToInt32(o); Session["IdUsuario"] = id; return id;
            }
        }
        private bool TienePermiso(string permiso)
        {
            var id = UsuarioActual(); if (!id.HasValue) return false;
            const string sql = @"SELECT COUNT(1) FROM dbo.USUARIO u
                JOIN dbo.ROL r ON r.id_rol=u.id_rol
                JOIN dbo.ROL_PERMISO rp ON rp.id_rol=r.id_rol
                JOIN dbo.PERMISO p ON p.id_permiso=rp.id_permiso
                WHERE u.id_usuario=@id AND u.estado=1 AND r.estado=1
                AND p.estado=1 AND p.nombre=@permiso";
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id.Value;
                cmd.Parameters.Add("@permiso", SqlDbType.NVarChar, 150).Value = permiso;
                cn.Open(); return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
        private ActionResult Denegado() { Response.StatusCode = 403; Response.TrySkipIisCustomErrors = true; return View("~/Views/Error/AccesoDenegado.cshtml"); }
        private void PermisosVista()
        {
            ViewBag.PuedeCrear = TienePermiso("CREAR_PRODUCTO_LIMPIEZA");
            ViewBag.PuedeEditar = TienePermiso("EDITAR_PRODUCTO_LIMPIEZA");
            ViewBag.PuedeMovimiento = TienePermiso("REGISTRAR_MOVIMIENTO_LIMPIEZA");
            ViewBag.PuedeVerMovimientos = TienePermiso("VER_MOVIMIENTOS_LIMPIEZA");
            ViewBag.PuedeReportes = TienePermiso("VER_REPORTES_LIMPIEZA");
        }
        private static InventarioProductoViewModel LeerProducto(SqlDataReader r)
        {
            return new InventarioProductoViewModel {
                IdProducto=Convert.ToInt32(r["id_producto"]), Nombre=Convert.ToString(r["nombre"]),
                Descripcion=r["descripcion"]==DBNull.Value?null:Convert.ToString(r["descripcion"]),
                Categoria=r["categoria"]==DBNull.Value?null:Convert.ToString(r["categoria"]),
                UnidadMedida=Convert.ToString(r["unidad_medida"]),
                StockActual=Convert.ToInt32(r["stock_actual"]), StockMinimo=Convert.ToInt32(r["stock_minimo"]),
                FechaVencimiento=r["fecha_vencimiento"]==DBNull.Value?(DateTime?)null:Convert.ToDateTime(r["fecha_vencimiento"]),
                Estado=Convert.ToBoolean(r["estado"])
            };
        }
        private InventarioProductoViewModel Obtener(int id)
        {
            using(var cn=new SqlConnection(cadena))
            using(var cmd=new SqlCommand("SELECT * FROM dbo.INVENTARIO WHERE id_producto=@id AND estado=1 AND ambito=N'LIMPIEZA'",cn))
            {
                cmd.Parameters.Add("@id",SqlDbType.Int).Value=id; cn.Open();
                using(var r=cmd.ExecuteReader()) return r.Read()?LeerProducto(r):null;
            }
        }
        [HttpGet]
        public ActionResult Index(string busqueda="", string categoria="")
        {
            if(!TienePermiso("VER_INVENTARIO_LIMPIEZA") && !TienePermiso("GESTIONAR_INVENTARIO_LIMPIEZA")) return Denegado();
            var lista=new List<InventarioProductoViewModel>();
            const string sql=@"SELECT * FROM dbo.INVENTARIO WHERE estado=1 AND ambito=N'LIMPIEZA'
                AND (@b='' OR nombre LIKE '%'+@b+'%' OR categoria LIKE '%'+@b+'%')
                AND (@c='' OR categoria=@c) ORDER BY nombre";
            using(var cn=new SqlConnection(cadena)) using(var cmd=new SqlCommand(sql,cn))
            {
                cmd.Parameters.Add("@b",SqlDbType.NVarChar,150).Value=busqueda??"";
                cmd.Parameters.Add("@c",SqlDbType.NVarChar,100).Value=categoria??"";
                cn.Open(); using(var r=cmd.ExecuteReader()) while(r.Read()) lista.Add(LeerProducto(r));
            }
            ViewBag.Busqueda=busqueda; ViewBag.Categoria=categoria; PermisosVista(); return View(lista);
        }
        [HttpGet]
        public ActionResult Detalle(int id)
        {
            if(!TienePermiso("VER_INVENTARIO_LIMPIEZA") && !TienePermiso("GESTIONAR_INVENTARIO_LIMPIEZA")) return Denegado();
            var m=Obtener(id); if(m==null) return HttpNotFound(); PermisosVista(); return View(m);
        }
        [HttpGet]
        public ActionResult Crear()
        {
            if(!TienePermiso("CREAR_PRODUCTO_LIMPIEZA")) return Denegado();
            return View(new InventarioProductoViewModel());
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult Crear(InventarioProductoViewModel m)
        {
            if(!TienePermiso("CREAR_PRODUCTO_LIMPIEZA")) return Denegado();
            if(m==null) return new HttpStatusCodeResult(400);
            if(!ModelState.IsValid) return View(m);
            const string sql=@"INSERT dbo.INVENTARIO(nombre,stock_actual,stock_minimo,unidad_medida,categoria,fecha_vencimiento,estado,descripcion,ambito)
                VALUES(@n,0,@min,@u,@c,@v,1,@d,N'LIMPIEZA')";
            using(var cn=new SqlConnection(cadena)) using(var cmd=new SqlCommand(sql,cn))
            {
                ParamProducto(cmd,m); cn.Open(); cmd.ExecuteNonQuery();
            }
            TempData["Success"]="Producto registrado. Registre una entrada para agregar existencias.";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public ActionResult Editar(int id)
        {
            if(!TienePermiso("EDITAR_PRODUCTO_LIMPIEZA")) return Denegado();
            var m=Obtener(id); return m==null?(ActionResult)HttpNotFound():View(m);
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult Editar(InventarioProductoViewModel m)
        {
            if(!TienePermiso("EDITAR_PRODUCTO_LIMPIEZA")) return Denegado();
            if(m==null) return new HttpStatusCodeResult(400);
            if(!ModelState.IsValid) return View(m);
            const string sql=@"UPDATE dbo.INVENTARIO SET nombre=@n,stock_minimo=@min,unidad_medida=@u,
                categoria=@c,fecha_vencimiento=@v,descripcion=@d
                WHERE id_producto=@id AND estado=1 AND ambito=N'LIMPIEZA'";
            using(var cn=new SqlConnection(cadena)) using(var cmd=new SqlCommand(sql,cn))
            {
                ParamProducto(cmd,m); cmd.Parameters.Add("@id",SqlDbType.Int).Value=m.IdProducto;
                cn.Open(); if(cmd.ExecuteNonQuery()==0) return HttpNotFound();
            }
            TempData["Success"]="Producto actualizado."; return RedirectToAction("Detalle",new{id=m.IdProducto});
        }
        private static void ParamProducto(SqlCommand cmd,InventarioProductoViewModel m)
        {
            cmd.Parameters.Add("@n",SqlDbType.NVarChar,150).Value=m.Nombre.Trim();
            cmd.Parameters.Add("@min",SqlDbType.Int).Value=m.StockMinimo;
            cmd.Parameters.Add("@u",SqlDbType.NVarChar,50).Value=m.UnidadMedida.Trim();
            cmd.Parameters.Add("@c",SqlDbType.NVarChar,100).Value=string.IsNullOrWhiteSpace(m.Categoria)?(object)DBNull.Value:m.Categoria.Trim();
            cmd.Parameters.Add("@d",SqlDbType.NVarChar,500).Value=string.IsNullOrWhiteSpace(m.Descripcion)?(object)DBNull.Value:m.Descripcion.Trim();
            cmd.Parameters.Add("@v",SqlDbType.Date).Value=m.FechaVencimiento.HasValue?(object)m.FechaVencimiento.Value.Date:DBNull.Value;
        }
        private sealed class ProductoExcel
        {
            public int Fila;
            public string Nombre, Categoria, Unidad, Descripcion;
            public int Minimo, Inicial;
            public DateTime? Vencimiento;
        }

        [HttpGet]
        public ActionResult PlantillaExcel()
        {
            if (!TienePermiso("CREAR_PRODUCTO_LIMPIEZA")) return Denegado();
            using (var libro = new XLWorkbook())
            {
                var hoja = libro.Worksheets.Add("Productos");
                string[] titulos = { "Nombre", "Categoria", "UnidadMedida", "StockMinimo", "FechaVencimiento", "Descripcion", "CantidadInicial" };
                for (int c = 0; c < titulos.Length; c++) hoja.Cell(1, c + 1).Value = titulos[c];
                hoja.Range(1, 1, 1, 7).Style.Font.Bold = true;
                hoja.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#133C55");
                hoja.Range(1, 1, 1, 7).Style.Font.FontColor = XLColor.White;
                hoja.Cell(2, 1).Value = "Desinfectante multiusos";
                hoja.Cell(2, 2).Value = "Desinfectantes";
                hoja.Cell(2, 3).Value = "Botella";
                hoja.Cell(2, 4).Value = 5;
                hoja.Cell(2, 5).Value = "";
                hoja.Cell(2, 6).Value = "Presentación de 1 litro";
                hoja.Cell(2, 7).Value = 12;
                hoja.Columns().AdjustToContents();
                using (var memoria = new MemoryStream())
                {
                    libro.SaveAs(memoria);
                    return File(memoria.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Inventario_Limpieza.xlsx");
                }
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ImportarExcel(HttpPostedFileBase archivoExcel)
        {
            if (!TienePermiso("CREAR_PRODUCTO_LIMPIEZA")) return Denegado();
            if (archivoExcel == null || archivoExcel.ContentLength == 0 ||
                !string.Equals(Path.GetExtension(archivoExcel.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase) ||
                archivoExcel.ContentLength > 5 * 1024 * 1024)
            {
                TempData["Error"] = "Seleccioná un archivo .xlsx válido de máximo 5 MB.";
                return RedirectToAction("Crear");
            }
            var filas = new List<ProductoExcel>();
            try
            {
                using (var libro = new XLWorkbook(archivoExcel.InputStream))
                {
                    var hoja = libro.Worksheet(1);
                    string[] columnas = { "Nombre", "Categoria", "UnidadMedida", "StockMinimo", "FechaVencimiento", "Descripcion", "CantidadInicial" };
                    for (int c = 1; c <= columnas.Length; c++)
                        if (!string.Equals(hoja.Cell(1, c).GetString().Trim(), columnas[c - 1], StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("Encabezado incorrecto en columna " + c + ". Descargá la plantilla oficial.");
                    var ultima = hoja.LastRowUsed();
                    if (ultima == null || ultima.RowNumber() < 2) throw new InvalidDataException("El Excel no contiene productos.");
                    if (ultima.RowNumber() > 501) throw new InvalidDataException("Máximo 500 productos por archivo.");
                    for (int n = 2; n <= ultima.RowNumber(); n++)
                    {
                        var r = hoja.Row(n);
                        if (r.Cells(1, 7).All(x => x.IsEmpty())) continue;
                        var p = new ProductoExcel
                        {
                            Fila = n,
                            Nombre = r.Cell(1).GetString().Trim(),
                            Categoria = r.Cell(2).GetString().Trim(),
                            Unidad = r.Cell(3).GetString().Trim(),
                            Descripcion = r.Cell(6).GetString().Trim()
                        };
                        if (p.Nombre.Length == 0 || p.Nombre.Length > 150 || p.Unidad.Length == 0 || p.Unidad.Length > 50 || p.Categoria.Length > 100 || p.Descripcion.Length > 500)
                            throw new InvalidDataException("Fila " + n + ": nombre, unidad o longitud de texto inválida.");
                        if (!int.TryParse(r.Cell(4).GetString(), out p.Minimo) || p.Minimo < 0)
                            throw new InvalidDataException("Fila " + n + ": StockMinimo debe ser entero no negativo.");
                        var cantidad = r.Cell(7).GetString().Trim();
                        if (cantidad.Length > 0 && (!int.TryParse(cantidad, out p.Inicial) || p.Inicial < 0))
                            throw new InvalidDataException("Fila " + n + ": CantidadInicial debe ser entero no negativo.");
                        var fecha = r.Cell(5).GetString().Trim();
                        if (fecha.Length > 0)
                        {
                            DateTime valor;
                            if (!DateTime.TryParse(fecha, CultureInfo.GetCultureInfo("es-CR"), DateTimeStyles.None, out valor) &&
                                !DateTime.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out valor))
                                throw new InvalidDataException("Fila " + n + ": fecha inválida (usar AAAA-MM-DD).");
                            p.Vencimiento = valor.Date;
                        }
                        filas.Add(p);
                    }
                }
                if (filas.Count == 0) throw new InvalidDataException("No se encontraron filas con productos.");
                var repetidos = filas.GroupBy(x => x.Nombre, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
                if (repetidos != null) throw new InvalidDataException("El Excel contiene nombres repetidos: " + repetidos.Key);
                if (filas.Any(x => x.Inicial > 0) && !TienePermiso("REGISTRAR_MOVIMIENTO_LIMPIEZA"))
                {
                    TempData["Error"] = "Para importar cantidades iniciales también necesitás el permiso de registrar movimientos.";
                    return RedirectToAction("Crear");
                }
                var usuario = UsuarioActual();
                if (!usuario.HasValue) return Denegado();
                using (var cn = new SqlConnection(cadena))
                {
                    cn.Open();
                    using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
                    {
                        try
                        {
                            foreach (var p in filas)
                            {
                                using (var duplicado = new SqlCommand("SELECT COUNT(*) FROM dbo.INVENTARIO WITH (UPDLOCK, HOLDLOCK) WHERE ambito=N'LIMPIEZA' AND nombre=@nombre", cn, tx))
                                {
                                    duplicado.Parameters.Add("@nombre", SqlDbType.NVarChar, 150).Value = p.Nombre;
                                    if (Convert.ToInt32(duplicado.ExecuteScalar()) > 0)
                                        throw new InvalidDataException("Fila " + p.Fila + ": ya existe el producto " + p.Nombre);
                                }
                                int id;
                                using (var cmd = new SqlCommand(@"INSERT INTO dbo.INVENTARIO(nombre,stock_actual,stock_minimo,unidad_medida,categoria,fecha_vencimiento,estado,descripcion,ambito)
                                    OUTPUT INSERTED.id_producto VALUES(@nombre,0,@min,@unidad,@categoria,@vence,1,@descripcion,N'LIMPIEZA')", cn, tx))
                                {
                                    cmd.Parameters.Add("@nombre", SqlDbType.NVarChar, 150).Value = p.Nombre;
                                    cmd.Parameters.Add("@min", SqlDbType.Int).Value = p.Minimo;
                                    cmd.Parameters.Add("@unidad", SqlDbType.NVarChar, 50).Value = p.Unidad;
                                    cmd.Parameters.Add("@categoria", SqlDbType.NVarChar, 100).Value = string.IsNullOrEmpty(p.Categoria) ? (object)DBNull.Value : p.Categoria;
                                    cmd.Parameters.Add("@vence", SqlDbType.Date).Value = p.Vencimiento.HasValue ? (object)p.Vencimiento.Value : DBNull.Value;
                                    cmd.Parameters.Add("@descripcion", SqlDbType.NVarChar, 500).Value = string.IsNullOrEmpty(p.Descripcion) ? (object)DBNull.Value : p.Descripcion;
                                    id = Convert.ToInt32(cmd.ExecuteScalar());
                                }
                                if (p.Inicial > 0)
                                {
                                    // Misma transacción: la entrada queda registrada y el stock se actualiza juntos.
                                    using (var cmd = new SqlCommand(@"INSERT INTO dbo.MOVIMIENTO_INVENTARIO(id_producto,id_usuario,tipo,cantidad,fecha,motivo,proveedor)
                                        VALUES(@id,@usuario,N'ENTRADA',@cantidad,SYSDATETIME(),N'Inventario inicial importado desde Excel',NULL);
                                        UPDATE dbo.INVENTARIO SET stock_actual=@cantidad WHERE id_producto=@id", cn, tx))
                                    {
                                        cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                                        cmd.Parameters.Add("@usuario", SqlDbType.Int).Value = usuario.Value;
                                        cmd.Parameters.Add("@cantidad", SqlDbType.Int).Value = p.Inicial;
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
                TempData["Success"] = "Se importaron correctamente " + filas.Count + " productos de limpieza.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se importó ningún producto. " + ex.Message;
                return RedirectToAction("Crear");
            }
        }
    



        [HttpGet]
        public ActionResult RegistrarMovimiento(int? id)
        {
            if(!TienePermiso("REGISTRAR_MOVIMIENTO_LIMPIEZA")) return Denegado();
            CargarProductos(); return View(new InventarioMovimientoViewModel{IdProducto=id??0,Tipo="ENTRADA"});
        }
        private void CargarProductos()
        {
            var lista=new List<SelectListItem>();
            using(var cn=new SqlConnection(cadena)) using(var cmd=new SqlCommand("SELECT id_producto,nombre FROM dbo.INVENTARIO WHERE estado=1 AND ambito=N'LIMPIEZA' ORDER BY nombre",cn))
            {
                cn.Open(); using(var r=cmd.ExecuteReader()) while(r.Read()) lista.Add(new SelectListItem{Value=Convert.ToString(r[0]),Text=Convert.ToString(r[1])});
            }
            ViewBag.Productos=lista;
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult RegistrarMovimiento(InventarioMovimientoViewModel m)
        {
            if(!TienePermiso("REGISTRAR_MOVIMIENTO_LIMPIEZA")) return Denegado();
            if(m==null) return new HttpStatusCodeResult(400);
            if(m.Tipo!="ENTRADA" && m.Tipo!="SALIDA") ModelState.AddModelError("Tipo","Seleccione un tipo válido.");
            if(!ModelState.IsValid) { CargarProductos(); return View(m); }
            var usuario=UsuarioActual(); if(!usuario.HasValue) return Denegado();
            try
            {
                using(var cn=new SqlConnection(cadena)) using(var cmd=new SqlCommand("dbo.SP_IL_REGISTRAR_MOVIMIENTO",cn))
                {
                    cmd.CommandType=CommandType.StoredProcedure;
                    cmd.Parameters.Add("@id_producto",SqlDbType.Int).Value=m.IdProducto;
                    cmd.Parameters.Add("@id_usuario",SqlDbType.Int).Value=usuario.Value;
                    cmd.Parameters.Add("@tipo",SqlDbType.NVarChar,20).Value=m.Tipo;
                    cmd.Parameters.Add("@cantidad",SqlDbType.Int).Value=m.Cantidad;
                    cmd.Parameters.Add("@proveedor",SqlDbType.NVarChar,150).Value=(object)m.Proveedor??DBNull.Value;
                    cmd.Parameters.Add("@motivo",SqlDbType.NVarChar,500).Value=(object)m.Motivo??DBNull.Value;
                    cn.Open(); cmd.ExecuteNonQuery();
                }
            }
            catch(SqlException ex) when(ex.Number>=51001 && ex.Number<=51006)
            {
                ModelState.AddModelError("",ex.Message); CargarProductos(); return View(m);
            }
            TempData["Success"]="Movimiento registrado correctamente."; return RedirectToAction("Movimientos");
        }
        [HttpGet]
        public ActionResult Movimientos()
        {
            if(!TienePermiso("VER_MOVIMIENTOS_LIMPIEZA")) return Denegado();
            var lista=new List<InventarioMovimientoViewModel>();
            const string sql=@"SELECT m.*,p.nombre AS producto,
                CONCAT(u.nombre,' ',u.apellido1,' ',u.apellido2) AS responsable
                FROM dbo.MOVIMIENTO_INVENTARIO m JOIN dbo.INVENTARIO p ON p.id_producto=m.id_producto AND p.ambito=N'LIMPIEZA'
                JOIN dbo.USUARIO u ON u.id_usuario=m.id_usuario ORDER BY m.fecha DESC,m.id_movimiento DESC";
            using(var cn=new SqlConnection(cadena)) using(var cmd=new SqlCommand(sql,cn))
            {
                cn.Open(); using(var r=cmd.ExecuteReader()) while(r.Read()) lista.Add(new InventarioMovimientoViewModel{
                    IdMovimiento=Convert.ToInt64(r["id_movimiento"]),IdProducto=Convert.ToInt32(r["id_producto"]),
                    NombreProducto=Convert.ToString(r["producto"]),Tipo=Convert.ToString(r["tipo"]),
                    Cantidad=Convert.ToInt32(r["cantidad"]),Fecha=Convert.ToDateTime(r["fecha"]),
                    Responsable=Convert.ToString(r["responsable"]),Proveedor=r["proveedor"]==DBNull.Value?null:Convert.ToString(r["proveedor"]),
                    Motivo=r["motivo"]==DBNull.Value?null:Convert.ToString(r["motivo"])});
            }
            PermisosVista(); return View(lista);
        }
        private InventarioReporteViewModel GenerarReporte(int anio,int mes)
        {
            var vm=new InventarioReporteViewModel{Anio=anio,Mes=mes};
            DateTime inicio=new DateTime(anio,mes,1), fin=inicio.AddMonths(1);
            const string sql=@"SELECT p.id_producto,p.nombre,p.unidad_medida,
                COALESCE(SUM(CASE WHEN m.fecha>=@ini AND m.fecha<@fin AND m.tipo='ENTRADA' THEN m.cantidad ELSE 0 END),0) AS entradas,
                COALESCE(SUM(CASE WHEN m.fecha>=@ini AND m.fecha<@fin AND m.tipo='SALIDA' THEN m.cantidad ELSE 0 END),0) AS salidas,
                p.stock_actual - COALESCE(SUM(CASE WHEN m.fecha>=@fin AND m.tipo='ENTRADA' THEN m.cantidad WHEN m.fecha>=@fin AND m.tipo='SALIDA' THEN -m.cantidad ELSE 0 END),0) AS saldo
                FROM dbo.INVENTARIO p LEFT JOIN dbo.MOVIMIENTO_INVENTARIO m ON m.id_producto=p.id_producto
                WHERE p.ambito=N'LIMPIEZA'
                GROUP BY p.id_producto,p.nombre,p.unidad_medida,p.stock_actual ORDER BY p.nombre";
            using(var cn=new SqlConnection(cadena)) using(var cmd=new SqlCommand(sql,cn))
            {
                cmd.Parameters.Add("@ini",SqlDbType.DateTime2).Value=inicio;
                cmd.Parameters.Add("@fin",SqlDbType.DateTime2).Value=fin;
                cn.Open(); using(var r=cmd.ExecuteReader()) while(r.Read()) vm.Filas.Add(new InventarioReporteFila{
                    IdProducto=Convert.ToInt32(r["id_producto"]),Nombre=Convert.ToString(r["nombre"]),Unidad=Convert.ToString(r["unidad_medida"]),
                    Entradas=Convert.ToInt32(r["entradas"]),Salidas=Convert.ToInt32(r["salidas"]),SaldoFinal=Convert.ToInt32(r["saldo"])});
            }
            return vm;
        }
        [HttpGet]
        public ActionResult Reportes(int? anio,int? mes)
        {
            if(!TienePermiso("VER_REPORTES_LIMPIEZA")) return Denegado();
            int a=anio??DateTime.Today.Year, m=mes??DateTime.Today.Month;
            if(a<2000||a>9998||m<1||m>12) return new HttpStatusCodeResult(400,"Período inválido.");
            return View(GenerarReporte(a,m));
        }
        [HttpGet]
        public ActionResult ExportarExcel(int anio, int mes)
        {
            if (!TienePermiso("VER_REPORTES_LIMPIEZA")) return Denegado();
            if (anio < 2000 || anio > 9998 || mes < 1 || mes > 12)
                return new HttpStatusCodeResult(400, "Período inválido.");

            var reporte = GenerarReporte(anio, mes);
            string nombreMes = new DateTime(anio, mes, 1)
                .ToString("MMMM", new CultureInfo("es-CR"));
            nombreMes = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(nombreMes);

            using (var libro = new XLWorkbook())
            {
                var hoja = libro.Worksheets.Add("Reporte de limpieza");
                hoja.ShowGridLines = false;
                hoja.Column("A").Width = 7;
                hoja.Column("B").Width = 39;
                hoja.Column("C").Width = 19;
                hoja.Column("D").Width = 17;
                hoja.Column("E").Width = 17;
                hoja.Column("F").Width = 19;
                hoja.Column("G").Width = 4;

                hoja.Range("A1:F2").Merge();
                hoja.Cell("A1").Value = "ESCUELA COPALCHÍ  |  AULA VIRTUAL";
                hoja.Range("A1:F2").Style.Fill.BackgroundColor = XLColor.FromHtml("#133C55");
                hoja.Range("A1:F2").Style.Font.FontColor = XLColor.White;
                hoja.Range("A1:F2").Style.Font.FontSize = 17;
                hoja.Range("A1:F2").Style.Font.Bold = true;
                hoja.Range("A1:F2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                hoja.Range("A1:F2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                hoja.Row(1).Height = 24;
                hoja.Row(2).Height = 24;

                hoja.Range("A3:F3").Merge();
                hoja.Cell("A3").Value = "REPORTE MENSUAL · INVENTARIO DE LIMPIEZA E HIGIENE";
                hoja.Range("A3:F3").Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F0F7");
                hoja.Range("A3:F3").Style.Font.FontColor = XLColor.FromHtml("#133C55");
                hoja.Range("A3:F3").Style.Font.Bold = true;
                hoja.Range("A3:F3").Style.Font.FontSize = 12;
                hoja.Range("A3:F3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                hoja.Row(3).Height = 28;

                hoja.Range("A5:B5").Merge();
                hoja.Cell("A5").Value = "PERÍODO: " + nombreMes + " " + anio;
                hoja.Range("D5:F5").Merge();
                hoja.Cell("D5").Value = "Generado: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                hoja.Range("A5:F5").Style.Font.FontColor = XLColor.FromHtml("#475569");
                hoja.Range("A5:F5").Style.Font.Bold = true;

                int totalEntradas = reporte.Filas.Sum(f => f.Entradas);
                int totalSalidas = reporte.Filas.Sum(f => f.Salidas);
                int totalSaldo = reporte.Filas.Sum(f => f.SaldoFinal);
                string[] indicadores = {
                    "PRODUCTOS: " + reporte.Filas.Count,
                    "ENTRADAS: " + totalEntradas,
                    "SALIDAS: " + totalSalidas
                };
                string[] rangos = { "A7:B8", "C7:D8", "E7:F8" };
                for (int i = 0; i < rangos.Length; i++)
                {
                    var rango = hoja.Range(rangos[i]);
                    rango.Merge();
                    rango.FirstCell().Value = indicadores[i];
                    rango.Style.Fill.BackgroundColor = XLColor.FromHtml(i == 2 ? "#EAF2F8" : "#F1F5F9");
                    rango.Style.Font.FontColor = XLColor.FromHtml("#133C55");
                    rango.Style.Font.Bold = true;
                    rango.Style.Font.FontSize = 12;
                    rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rango.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                hoja.Row(7).Height = 20;
                hoja.Row(8).Height = 20;

                string[] columnas = { "N.º", "PRODUCTO", "UNIDAD DE MEDIDA", "ENTRADAS", "SALIDAS", "SALDO FINAL" };
                for (int c = 0; c < columnas.Length; c++)
                    hoja.Cell(10, c + 1).Value = columnas[c];
                var encabezado = hoja.Range(10, 1, 10, 6);
                encabezado.Style.Fill.BackgroundColor = XLColor.FromHtml("#133C55");
                encabezado.Style.Font.FontColor = XLColor.White;
                encabezado.Style.Font.Bold = true;
                encabezado.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                encabezado.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                hoja.Row(10).Height = 30;

                int fila = 11;
                foreach (var item in reporte.Filas)
                {
                    hoja.Cell(fila, 1).Value = fila - 10;
                    hoja.Cell(fila, 2).Value = item.Nombre ?? "";
                    hoja.Cell(fila, 3).Value = item.Unidad ?? "";
                    hoja.Cell(fila, 4).Value = item.Entradas;
                    hoja.Cell(fila, 5).Value = item.Salidas;
                    hoja.Cell(fila, 6).Value = item.SaldoFinal;
                    var rangoFila = hoja.Range(fila, 1, fila, 6);
                    rangoFila.Style.Fill.BackgroundColor = XLColor.FromHtml(
                        fila % 2 == 0 ? "#F1F5F9" : "#FFFFFF");
                    rangoFila.Style.Font.FontColor = XLColor.FromHtml("#334155");
                    rangoFila.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    hoja.Range(fila, 4, fila, 6).Style.NumberFormat.Format = "#,##0;[Red](#,##0);–";
                    hoja.Range(fila, 1, fila, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    hoja.Range(fila, 4, fila, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    hoja.Row(fila).Height = 23;
                    fila++;
                }
                if (reporte.Filas.Count == 0)
                {
                    hoja.Range(11, 1, 11, 6).Merge();
                    hoja.Cell(11, 1).Value = "No se encontraron productos para este período.";
                    hoja.Range(11, 1, 11, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    fila = 12;
                }
                else
                {
                    hoja.Range(10, 1, fila - 1, 6).SetAutoFilter();
                }

                hoja.Range(fila + 1, 1, fila + 1, 3).Merge();
                hoja.Cell(fila + 1, 1).Value = "TOTALES DEL REPORTE";
                hoja.Cell(fila + 1, 4).Value = totalEntradas;
                hoja.Cell(fila + 1, 5).Value = totalSalidas;
                hoja.Cell(fila + 1, 6).Value = totalSaldo;
                var totales = hoja.Range(fila + 1, 1, fila + 1, 6);
                totales.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEAF3");
                totales.Style.Font.FontColor = XLColor.FromHtml("#133C55");
                totales.Style.Font.Bold = true;
                hoja.Row(fila + 1).Height = 28;
                hoja.Range(fila + 1, 4, fila + 1, 6).Style.NumberFormat.Format = "#,##0";

                hoja.Range(fila + 3, 1, fila + 3, 6).Merge();
                hoja.Cell(fila + 3, 1).Value =
                    "Nota: el saldo final corresponde al cierre del período seleccionado. " +
                    "El total suma unidades de medida que pueden ser diferentes.";
                hoja.Cell(fila + 3, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");
                hoja.Cell(fila + 3, 1).Style.Font.Italic = true;
                hoja.Cell(fila + 3, 1).Style.Font.FontSize = 9;
                hoja.SheetView.FreezeRows(10);
                hoja.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                hoja.PageSetup.FitToPages(1, 0);
                hoja.PageSetup.Margins.Left = 0.25;
                hoja.PageSetup.Margins.Right = 0.25;

                using (var memoria = new MemoryStream())
                {
                    libro.SaveAs(memoria);
                    return File(memoria.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Inventario_Limpieza_" + anio + "_" + mes.ToString("00") + ".xlsx");
                }
            }
        }
        [HttpGet]
        public ActionResult ExportarCsv(int anio,int mes)
        {
            if(!TienePermiso("VER_REPORTES_LIMPIEZA")) return Denegado();
            if(anio<2000||anio>9998||mes<1||mes>12) return new HttpStatusCodeResult(400);
            var vm=GenerarReporte(anio,mes); var sb=new StringBuilder();
            sb.AppendLine("Producto;Unidad;Entradas;Salidas;Saldo final");
            foreach(var f in vm.Filas)
            {
                Func<string,string> safe=s=>"\""+(s??"").Replace("\"","\"\"").Replace("\r"," ").Replace("\n"," ")+"\"";
                sb.AppendLine(string.Join(";",safe(f.Nombre),safe(f.Unidad),f.Entradas,f.Salidas,f.SaldoFinal));
            }
            return File(new UTF8Encoding(true).GetBytes(sb.ToString()),"text/csv","Inventario_"+anio+"_"+mes+".csv");
        }
    }
}
