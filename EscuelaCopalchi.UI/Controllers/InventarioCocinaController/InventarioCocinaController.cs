using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using ClosedXML.Excel;
using EscuelaCopalchi.UI.Models;

namespace EscuelaCopalchi.UI.Controllers
{
    [Authorize]
    public class InventarioCocinaController : Controller
    {
        private readonly string cadena = ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"].ConnectionString;

        private int? UsuarioActual()
        {
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
                cn.Open();
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return null;
                id = Convert.ToInt32(result);
                Session["IdUsuario"] = id;
                return id;
            }
        }

        private bool TienePermiso(string permiso)
        {
            var usuario = UsuarioActual();
            if (!usuario.HasValue) return false;
            const string sql = @"SELECT COUNT(1) FROM dbo.USUARIO u
                JOIN dbo.ROL r ON r.id_rol=u.id_rol
                JOIN dbo.ROL_PERMISO rp ON rp.id_rol=r.id_rol
                JOIN dbo.PERMISO p ON p.id_permiso=rp.id_permiso
                WHERE u.id_usuario=@id AND u.estado=1 AND r.estado=1
                AND p.estado=1 AND p.nombre=@permiso";
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = usuario.Value;
                cmd.Parameters.Add("@permiso", SqlDbType.NVarChar, 150).Value = permiso;
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private bool PuedeVer() { return TienePermiso("VER_INVENTARIO_COCINA") || TienePermiso("GESTIONAR_INVENTARIO_COCINA"); }
        private ActionResult Denegado()
        {
            Response.StatusCode = 403;
            Response.TrySkipIisCustomErrors = true;
            return View("~/Views/Error/AccesoDenegado.cshtml");
        }
        private void PermisosVista()
        {
            ViewBag.PuedeCrear = TienePermiso("CREAR_PRODUCTO_COCINA");
            ViewBag.PuedeEditar = TienePermiso("EDITAR_PRODUCTO_COCINA");
            ViewBag.PuedeMovimiento = TienePermiso("REGISTRAR_MOVIMIENTO_COCINA");
            ViewBag.PuedeVerMovimientos = TienePermiso("VER_MOVIMIENTOS_COCINA");
            ViewBag.PuedeReportes = TienePermiso("VER_REPORTES_COCINA");
        }

        private static InventarioCocinaProductoViewModel LeerProducto(SqlDataReader r)
        {
            return new InventarioCocinaProductoViewModel
            {
                IdProducto = Convert.ToInt32(r["id_producto"]),
                Nombre = Convert.ToString(r["nombre"]),
                Descripcion = r["descripcion"] == DBNull.Value ? null : Convert.ToString(r["descripcion"]),
                Categoria = r["categoria"] == DBNull.Value ? null : Convert.ToString(r["categoria"]),
                UnidadMedida = Convert.ToString(r["unidad_medida"]),
                StockActual = Convert.ToInt32(r["stock_actual"]),
                StockMinimo = Convert.ToInt32(r["stock_minimo"]),
                FechaVencimiento = r["fecha_vencimiento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["fecha_vencimiento"]),
                Estado = Convert.ToBoolean(r["estado"])
            };
        }
        private const string ProductoSql = @"SELECT id_producto,nombre,descripcion,categoria,unidad_medida,
            stock_actual,stock_minimo,fecha_vencimiento,estado FROM dbo.INVENTARIO";

        private InventarioCocinaProductoViewModel Obtener(int id)
        {
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(ProductoSql + " WHERE id_producto=@id AND ambito=N'COCINA' AND estado=1", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                cn.Open();
                using (var r = cmd.ExecuteReader()) return r.Read() ? LeerProducto(r) : null;
            }
        }

        [HttpGet]
        public ActionResult Index(string busqueda = "", string categoria = "", string estado = "")
        {
            if (!PuedeVer()) return Denegado();
            var lista = new List<InventarioCocinaProductoViewModel>();
            const string sql = ProductoSql + @" WHERE estado=1 AND ambito=N'COCINA'
                AND (@b=N'' OR nombre LIKE N'%'+@b+N'%' OR categoria LIKE N'%'+@b+N'%')
                AND (@c=N'' OR categoria=@c) ORDER BY nombre";
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@b", SqlDbType.NVarChar, 150).Value = busqueda ?? "";
                cmd.Parameters.Add("@c", SqlDbType.NVarChar, 100).Value = categoria ?? "";
                cn.Open();
                using (var r = cmd.ExecuteReader()) while (r.Read()) lista.Add(LeerProducto(r));
            }
            // La fecha del producto es solo informativa. El estado real se obtiene de la vista de lotes.
            CargarEstadosLotes(lista);
            if (!string.IsNullOrWhiteSpace(estado))
                lista = lista.Where(x => string.Equals((ViewBag.EstadosPorProducto as Dictionary<int, string>)[x.IdProducto], estado, StringComparison.OrdinalIgnoreCase)).ToList();
            ViewBag.Busqueda = busqueda;
            ViewBag.Categoria = categoria;
            ViewBag.EstadoFiltro = estado;
            PermisosVista();
            return View(new InventarioCocinaIndexViewModel { Productos = lista });
        }

        private void CargarEstadosLotes(List<InventarioCocinaProductoViewModel> productos)
        {
            // Se mantienen los modelos compatibles; EstadoProducto se calcula con la fecha
            // de vencimiento representativa del lote disponible más próximo.
            const string sql = @"SELECT id_producto, proximo_vencimiento, estado_producto
                                 FROM dbo.VW_INVENTARIO_COCINA_ESTADO";
            var datos = new Dictionary<int, Tuple<DateTime?, string>>();
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cn.Open();
                using (var r = cmd.ExecuteReader()) while (r.Read())
                    datos[Convert.ToInt32(r["id_producto"])] = Tuple.Create(
                        r["proximo_vencimiento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["proximo_vencimiento"]),
                        Convert.ToString(r["estado_producto"]));
            }
            // Para conservar el estado exacto de la vista, se expone por ViewBag en Index.
            var estados = new Dictionary<int, string>();
            foreach (var p in productos)
            {
                Tuple<DateTime?, string> dato;
                if (datos.TryGetValue(p.IdProducto, out dato))
                {
                    p.FechaVencimiento = dato.Item1;
                    estados[p.IdProducto] = dato.Item2;
                }
            }
            ViewBag.EstadosPorProducto = estados;
        }

        [HttpGet]
        public ActionResult Detalle(int id)
        {
            if (!PuedeVer()) return Denegado();
            var p = Obtener(id);
            if (p == null) return HttpNotFound();
            CargarEstadosLotes(new List<InventarioCocinaProductoViewModel> { p });
            ViewBag.Lotes = ObtenerLotes(id);
            PermisosVista();
            return View(p);
        }

        private List<InventarioCocinaLoteViewModel> ObtenerLotes(int id)
        {
            var lista = new List<InventarioCocinaLoteViewModel>();
            const string sql = @"SELECT l.id_lote,l.id_producto,p.nombre,l.cantidad_disponible,
                l.fecha_vencimiento,l.fecha_ingreso,l.proveedor
                FROM dbo.INVENTARIO_COCINA_LOTE l
                JOIN dbo.INVENTARIO p ON p.id_producto=l.id_producto
                WHERE l.id_producto=@id AND p.ambito=N'COCINA'
                ORDER BY CASE WHEN l.fecha_vencimiento IS NULL THEN 1 ELSE 0 END,
                l.fecha_vencimiento,l.fecha_ingreso";
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                cn.Open();
                using (var r = cmd.ExecuteReader()) while (r.Read()) lista.Add(new InventarioCocinaLoteViewModel
                {
                    IdLote = Convert.ToInt32(r["id_lote"]),
                    IdProducto = Convert.ToInt32(r["id_producto"]),
                    NombreProducto = Convert.ToString(r["nombre"]),
                    Cantidad = Convert.ToInt32(r["cantidad_disponible"]),
                    FechaVencimiento = r["fecha_vencimiento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["fecha_vencimiento"]),
                    FechaIngreso = Convert.ToDateTime(r["fecha_ingreso"]),
                    Proveedor = r["proveedor"] == DBNull.Value ? null : Convert.ToString(r["proveedor"])
                });
            }
            return lista;
        }

        [HttpGet]
        public ActionResult Crear()
        {
            if (!TienePermiso("CREAR_PRODUCTO_COCINA")) return Denegado();
            return View(new InventarioCocinaProductoViewModel { Estado = true });
        }

        private static void ValidarProducto(ModelStateDictionary state, InventarioCocinaProductoViewModel p)
        {
            if (p == null) return;
            if (string.IsNullOrWhiteSpace(p.Nombre)) state.AddModelError("Nombre", "Indique el nombre.");
            if (string.IsNullOrWhiteSpace(p.UnidadMedida)) state.AddModelError("UnidadMedida", "Indique la unidad.");
        }
        private static void ParamProducto(SqlCommand cmd, InventarioCocinaProductoViewModel p)
        {
            cmd.Parameters.Add("@n", SqlDbType.NVarChar, 150).Value = p.Nombre.Trim();
            cmd.Parameters.Add("@u", SqlDbType.NVarChar, 50).Value = p.UnidadMedida.Trim();
            cmd.Parameters.Add("@min", SqlDbType.Int).Value = p.StockMinimo;
            cmd.Parameters.Add("@c", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(p.Categoria) ? (object)DBNull.Value : p.Categoria.Trim();
            cmd.Parameters.Add("@d", SqlDbType.NVarChar, 500).Value = string.IsNullOrWhiteSpace(p.Descripcion) ? (object)DBNull.Value : p.Descripcion.Trim();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Crear(InventarioCocinaProductoViewModel p)
        {
            if (!TienePermiso("CREAR_PRODUCTO_COCINA")) return Denegado();
            if (p == null) return new HttpStatusCodeResult(400);
            ValidarProducto(ModelState, p);
            if (!ModelState.IsValid) return View(p);
            const string sql = @"INSERT dbo.INVENTARIO
                (nombre,stock_actual,stock_minimo,unidad_medida,categoria,fecha_vencimiento,estado,descripcion,ambito)
                VALUES (@n,0,@min,@u,@c,NULL,1,@d,N'COCINA')";
            try
            {
                using (var cn = new SqlConnection(cadena))
                using (var cmd = new SqlCommand(sql, cn))
                {
                    ParamProducto(cmd, p);
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                ModelState.AddModelError("Nombre", "Ya existe un producto con ese nombre.");
                return View(p);
            }
            TempData["Success"] = "Producto registrado. Registre una entrada para agregar existencias y vencimiento.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Editar(int id)
        {
            if (!TienePermiso("EDITAR_PRODUCTO_COCINA")) return Denegado();
            var p = Obtener(id);
            return p == null ? (ActionResult)HttpNotFound() : View(p);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Editar(InventarioCocinaProductoViewModel p)
        {
            if (!TienePermiso("EDITAR_PRODUCTO_COCINA")) return Denegado();
            if (p == null) return new HttpStatusCodeResult(400);
            ValidarProducto(ModelState, p);
            if (!ModelState.IsValid) return View(p);
            const string sql = @"UPDATE dbo.INVENTARIO SET nombre=@n,stock_minimo=@min,
                unidad_medida=@u,categoria=@c,descripcion=@d
                WHERE id_producto=@id AND estado=1 AND ambito=N'COCINA'";
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                ParamProducto(cmd, p);
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = p.IdProducto;
                cn.Open();
                if (cmd.ExecuteNonQuery() == 0) return HttpNotFound();
            }
            TempData["Success"] = "Producto actualizado.";
            return RedirectToAction("Detalle", new { id = p.IdProducto });
        }

        private void CargarProductos()
        {
            var items = new List<SelectListItem>();
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand("SELECT id_producto,nombre FROM dbo.INVENTARIO WHERE estado=1 AND ambito=N'COCINA' ORDER BY nombre", cn))
            {
                cn.Open();
                using (var r = cmd.ExecuteReader()) while (r.Read()) items.Add(new SelectListItem
                {
                    Value = Convert.ToString(r[0]),
                    Text = Convert.ToString(r[1])
                });
            }
            ViewBag.Productos = items;
        }

        [HttpGet]
        public ActionResult RegistrarMovimiento(int? id)
        {
            if (!TienePermiso("REGISTRAR_MOVIMIENTO_COCINA")) return Denegado();
            CargarProductos();
            return View(new InventarioCocinaMovimientoViewModel { IdProducto = id ?? 0, Tipo = "ENTRADA" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult RegistrarMovimiento(InventarioCocinaMovimientoViewModel m)
        {
            if (!TienePermiso("REGISTRAR_MOVIMIENTO_COCINA")) return Denegado();
            if (m == null) return new HttpStatusCodeResult(400);
            if (m.Tipo != "ENTRADA" && m.Tipo != "SALIDA")
                ModelState.AddModelError("Tipo", "Seleccione ENTRADA o SALIDA.");
            if (m.Tipo == "ENTRADA" && m.FechaVencimiento.HasValue && m.FechaVencimiento.Value.Date < DateTime.Today)
                ModelState.AddModelError("FechaVencimiento", "No se pueden ingresar lotes vencidos.");
            if (!ModelState.IsValid) { CargarProductos(); return View(m); }
            var usuario = UsuarioActual();
            if (!usuario.HasValue) return Denegado();
            try
            {
                using (var cn = new SqlConnection(cadena))
                using (var cmd = new SqlCommand("dbo.SP_IC_REGISTRAR_MOVIMIENTO", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@id_producto", SqlDbType.Int).Value = m.IdProducto;
                    cmd.Parameters.Add("@id_usuario", SqlDbType.Int).Value = usuario.Value;
                    cmd.Parameters.Add("@tipo", SqlDbType.NVarChar, 20).Value = m.Tipo;
                    cmd.Parameters.Add("@cantidad", SqlDbType.Int).Value = m.Cantidad;
                    cmd.Parameters.Add("@proveedor", SqlDbType.NVarChar, 150).Value = string.IsNullOrWhiteSpace(m.Proveedor) ? (object)DBNull.Value : m.Proveedor.Trim();
                    cmd.Parameters.Add("@motivo", SqlDbType.NVarChar, 500).Value = string.IsNullOrWhiteSpace(m.Motivo) ? (object)DBNull.Value : m.Motivo.Trim();
                    cmd.Parameters.Add("@fecha_vencimiento", SqlDbType.Date).Value = m.Tipo == "ENTRADA" && m.FechaVencimiento.HasValue ? (object)m.FechaVencimiento.Value.Date : DBNull.Value;
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException ex) when (ex.Number >= 51101 && ex.Number <= 51109)
            {
                ModelState.AddModelError("", ex.Message);
                CargarProductos();
                return View(m);
            }
            TempData["Success"] = "Movimiento registrado correctamente.";
            return RedirectToAction("Movimientos");
        }

        [HttpGet]
        public ActionResult Movimientos()
        {
            if (!TienePermiso("VER_MOVIMIENTOS_COCINA")) return Denegado();
            var lista = new List<InventarioCocinaMovimientoViewModel>();
            const string sql = @"SELECT m.*,p.nombre AS producto,
                CONCAT(u.nombre,' ',u.apellido1,' ',u.apellido2) AS responsable
                FROM dbo.MOVIMIENTO_INVENTARIO m
                JOIN dbo.INVENTARIO p ON p.id_producto=m.id_producto AND p.ambito=N'COCINA'
                JOIN dbo.USUARIO u ON u.id_usuario=m.id_usuario
                ORDER BY m.fecha DESC,m.id_movimiento DESC";
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cn.Open();
                using (var r = cmd.ExecuteReader()) while (r.Read()) lista.Add(new InventarioCocinaMovimientoViewModel
                {
                    IdMovimiento = Convert.ToInt64(r["id_movimiento"]),
                    IdProducto = Convert.ToInt32(r["id_producto"]),
                    NombreProducto = Convert.ToString(r["producto"]),
                    Tipo = Convert.ToString(r["tipo"]),
                    Cantidad = Convert.ToInt32(r["cantidad"]),
                    Fecha = Convert.ToDateTime(r["fecha"]),
                    Responsable = Convert.ToString(r["responsable"]),
                    Proveedor = r["proveedor"] == DBNull.Value ? null : Convert.ToString(r["proveedor"]),
                    Motivo = r["motivo"] == DBNull.Value ? null : Convert.ToString(r["motivo"])
                });
            }
            PermisosVista();
            return View(lista);
        }

        private InventarioReporteViewModel GenerarReporte(int anio, int mes)
        {
            var vm = new InventarioReporteViewModel { Anio = anio, Mes = mes };
            DateTime inicio = new DateTime(anio, mes, 1), fin = inicio.AddMonths(1);
            const string sql = @"SELECT p.id_producto,p.nombre,p.unidad_medida,
                COALESCE(SUM(CASE WHEN m.fecha>=@ini AND m.fecha<@fin AND m.tipo=N'ENTRADA' THEN m.cantidad ELSE 0 END),0) AS entradas,
                COALESCE(SUM(CASE WHEN m.fecha>=@ini AND m.fecha<@fin AND m.tipo=N'SALIDA' THEN m.cantidad ELSE 0 END),0) AS salidas,
                p.stock_actual - COALESCE(SUM(CASE WHEN m.fecha>=@fin AND m.tipo=N'ENTRADA' THEN m.cantidad WHEN m.fecha>=@fin AND m.tipo=N'SALIDA' THEN -m.cantidad ELSE 0 END),0) AS saldo
                FROM dbo.INVENTARIO p LEFT JOIN dbo.MOVIMIENTO_INVENTARIO m ON m.id_producto=p.id_producto
                WHERE p.ambito=N'COCINA'
                GROUP BY p.id_producto,p.nombre,p.unidad_medida,p.stock_actual ORDER BY p.nombre";
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@ini", SqlDbType.DateTime2).Value = inicio;
                cmd.Parameters.Add("@fin", SqlDbType.DateTime2).Value = fin;
                cn.Open();
                using (var r = cmd.ExecuteReader()) while (r.Read()) vm.Filas.Add(new InventarioReporteFila
                {
                    IdProducto = Convert.ToInt32(r["id_producto"]),
                    Nombre = Convert.ToString(r["nombre"]),
                    Unidad = Convert.ToString(r["unidad_medida"]),
                    Entradas = Convert.ToInt32(r["entradas"]),
                    Salidas = Convert.ToInt32(r["salidas"]),
                    SaldoFinal = Convert.ToInt32(r["saldo"])
                });
            }
            return vm;
        }

        [HttpGet]
        public ActionResult Reportes(int? anio, int? mes)
        {
            if (!TienePermiso("VER_REPORTES_COCINA")) return Denegado();
            int a = anio ?? DateTime.Today.Year, m = mes ?? DateTime.Today.Month;
            if (a < 2000 || a > 9998 || m < 1 || m > 12) return new HttpStatusCodeResult(400);
            return View(GenerarReporte(a, m));
        }

        [HttpGet]
        public ActionResult ExportarExcel(int anio, int mes)
        {
            if (!TienePermiso("VER_REPORTES_COCINA")) return Denegado();
            if (anio < 2000 || anio > 9998 || mes < 1 || mes > 12) return new HttpStatusCodeResult(400);
            var vm = GenerarReporte(anio, mes);
            using (var libro = new XLWorkbook())
            {
                var hoja = libro.Worksheets.Add("Inventario cocina");
                hoja.Cell(1, 1).Value = "ESCUELA COPALCHÍ | INVENTARIO DE COCINA";
                hoja.Range(1, 1, 1, 5).Merge();
                hoja.Range(1, 1, 1, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#133C55");
                hoja.Range(1, 1, 1, 5).Style.Font.FontColor = XLColor.White;
                hoja.Range(1, 1, 1, 5).Style.Font.Bold = true;
                hoja.Cell(2, 1).Value = "Período: " + new DateTime(anio, mes, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("es-CR"));
                string[] headers = { "Producto", "Unidad", "Entradas", "Salidas", "Saldo final" };
                for (int i = 0; i < headers.Length; i++) hoja.Cell(4, i + 1).Value = headers[i];
                hoja.Range(4, 1, 4, 5).Style.Font.Bold = true;
                int fila = 5;
                foreach (var f in vm.Filas)
                {
                    hoja.Cell(fila, 1).Value = f.Nombre;
                    hoja.Cell(fila, 2).Value = f.Unidad;
                    hoja.Cell(fila, 3).Value = f.Entradas;
                    hoja.Cell(fila, 4).Value = f.Salidas;
                    hoja.Cell(fila, 5).Value = f.SaldoFinal;
                    fila++;
                }
                hoja.Columns().AdjustToContents();
                using (var memoria = new MemoryStream())
                {
                    libro.SaveAs(memoria);
                    return File(memoria.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Inventario_Cocina_" + anio + "_" + mes.ToString("00") + ".xlsx");
                }
            }
        }

        [HttpGet]
        public ActionResult ExportarCsv(int anio, int mes)
        {
            if (!TienePermiso("VER_REPORTES_COCINA")) return Denegado();
            if (anio < 2000 || anio > 9998 || mes < 1 || mes > 12) return new HttpStatusCodeResult(400);
            var vm = GenerarReporte(anio, mes);
            var sb = new StringBuilder("Producto;Unidad;Entradas;Salidas;Saldo final\r\n");
            foreach (var f in vm.Filas)
            {
                sb.AppendLine(string.Join(";", CsvSeguro(f.Nombre), CsvSeguro(f.Unidad), f.Entradas, f.Salidas, f.SaldoFinal));
            }
            return File(new UTF8Encoding(true).GetBytes(sb.ToString()), "text/csv", "Inventario_Cocina_" + anio + "_" + mes.ToString("00") + ".csv");
        }
        private static string CsvSeguro(string s)
        {
            s = (s ?? "").Replace("\r", " ").Replace("\n", " ");
            if (s.Length > 0 && "=+-@".Contains(s[0])) s = "'" + s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        [HttpGet]
        public ActionResult PlantillaExcel()
        {
            if (!TienePermiso("CREAR_PRODUCTO_COCINA")) return Denegado();
            using (var libro = new XLWorkbook())
            {
                var hoja = libro.Worksheets.Add("Productos");
                string[] columnas = { "Nombre", "Categoria", "UnidadMedida", "StockMinimo", "FechaVencimiento", "Descripcion", "CantidadInicial" };
                for (int i = 0; i < columnas.Length; i++) hoja.Cell(1, i + 1).Value = columnas[i];
                hoja.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#133C55");
                hoja.Range(1, 1, 1, 7).Style.Font.FontColor = XLColor.White;
                hoja.Range(1, 1, 1, 7).Style.Font.Bold = true;
                hoja.Cell(2, 1).Value = "Arroz";
                hoja.Cell(2, 2).Value = "Granos";
                hoja.Cell(2, 3).Value = "kg";
                hoja.Cell(2, 4).Value = 5;
                hoja.Cell(2, 5).Value = "2027-06-30";
                hoja.Cell(2, 6).Value = "Presentación por kilogramo";
                hoja.Cell(2, 7).Value = 10;
                hoja.Columns().AdjustToContents();
                using (var ms = new MemoryStream())
                {
                    libro.SaveAs(ms);
                    return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Inventario_Cocina.xlsx");
                }
            }
        }

        private sealed class FilaExcel
        {
            public int Numero;
            public string Nombre, Categoria, Unidad, Descripcion;
            public int Minimo, Inicial;
            public DateTime? Vencimiento;
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ImportarExcel(HttpPostedFileBase archivoExcel)
        {
            if (!TienePermiso("CREAR_PRODUCTO_COCINA")) return Denegado();
            if (archivoExcel == null || archivoExcel.ContentLength <= 0 || archivoExcel.ContentLength > 5 * 1024 * 1024 ||
                !string.Equals(Path.GetExtension(archivoExcel.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Seleccione un archivo .xlsx de máximo 5 MB.";
                return RedirectToAction("Crear");
            }
            var filas = new List<FilaExcel>();
            try
            {
                using (var libro = new XLWorkbook(archivoExcel.InputStream))
                {
                    var hoja = libro.Worksheet(1);
                    string[] columnas = { "Nombre", "Categoria", "UnidadMedida", "StockMinimo", "FechaVencimiento", "Descripcion", "CantidadInicial" };
                    for (int i = 0; i < columnas.Length; i++)
                        if (!string.Equals(hoja.Cell(1, i + 1).GetString().Trim(), columnas[i], StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("Encabezado inválido en columna " + (i + 1));
                    var ultima = hoja.LastRowUsed();
                    if (ultima == null || ultima.RowNumber() < 2) throw new InvalidDataException("No hay productos.");
                    if (ultima.RowNumber() > 501) throw new InvalidDataException("Máximo 500 productos.");
                    for (int n = 2; n <= ultima.RowNumber(); n++)
                    {
                        var r = hoja.Row(n);
                        if (r.Cells(1, 7).All(c => c.IsEmpty())) continue;
                        var p = new FilaExcel
                        {
                            Numero = n,
                            Nombre = r.Cell(1).GetString().Trim(),
                            Categoria = r.Cell(2).GetString().Trim(),
                            Unidad = r.Cell(3).GetString().Trim(),
                            Descripcion = r.Cell(6).GetString().Trim()
                        };
                        if (p.Nombre.Length == 0 || p.Nombre.Length > 150 || p.Unidad.Length == 0 || p.Unidad.Length > 50 || p.Categoria.Length > 100 || p.Descripcion.Length > 500)
                            throw new InvalidDataException("Fila " + n + ": texto inválido.");
                        if (!int.TryParse(r.Cell(4).GetString(), out p.Minimo) || p.Minimo < 0)
                            throw new InvalidDataException("Fila " + n + ": stock mínimo inválido.");
                        string inicial = r.Cell(7).GetString().Trim();
                        if (inicial.Length > 0 && (!int.TryParse(inicial, out p.Inicial) || p.Inicial < 0))
                            throw new InvalidDataException("Fila " + n + ": cantidad inicial inválida.");
                        string fecha = r.Cell(5).GetString().Trim();
                        if (fecha.Length > 0)
                        {
                            DateTime valor;
                            if (!DateTime.TryParseExact(fecha, new[] { "yyyy-MM-dd", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out valor))
                                throw new InvalidDataException("Fila " + n + ": fecha inválida. Use AAAA-MM-DD.");
                            p.Vencimiento = valor.Date;
                        }
                        if (p.Inicial > 0 && p.Vencimiento.HasValue && p.Vencimiento.Value.Date < DateTime.Today)
                            throw new InvalidDataException("Fila " + n + ": el lote inicial está vencido.");
                        filas.Add(p);
                    }
                }
                if (filas.Count == 0) throw new InvalidDataException("No hay productos.");
                var repetido = filas.GroupBy(x => x.Nombre, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
                if (repetido != null) throw new InvalidDataException("Producto repetido: " + repetido.Key);
                if (filas.Any(x => x.Inicial > 0) && !TienePermiso("REGISTRAR_MOVIMIENTO_COCINA"))
                {
                    TempData["Error"] = "No tiene permiso para importar cantidades iniciales.";
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
                                using (var check = new SqlCommand("SELECT COUNT(*) FROM dbo.INVENTARIO WITH (UPDLOCK,HOLDLOCK) WHERE ambito=N'COCINA' AND nombre=@n", cn, tx))
                                {
                                    check.Parameters.Add("@n", SqlDbType.NVarChar, 150).Value = p.Nombre;
                                    if (Convert.ToInt32(check.ExecuteScalar()) > 0) throw new InvalidDataException("Fila " + p.Numero + ": ya existe " + p.Nombre);
                                }
                                int id;
                                using (var cmd = new SqlCommand(@"INSERT dbo.INVENTARIO
                                    (nombre,stock_actual,stock_minimo,unidad_medida,categoria,fecha_vencimiento,estado,descripcion,ambito)
                                    OUTPUT INSERTED.id_producto VALUES(@n,0,@min,@u,@c,NULL,1,@d,N'COCINA')", cn, tx))
                                {
                                    cmd.Parameters.Add("@n", SqlDbType.NVarChar, 150).Value = p.Nombre;
                                    cmd.Parameters.Add("@min", SqlDbType.Int).Value = p.Minimo;
                                    cmd.Parameters.Add("@u", SqlDbType.NVarChar, 50).Value = p.Unidad;
                                    cmd.Parameters.Add("@c", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(p.Categoria) ? (object)DBNull.Value : p.Categoria;
                                    cmd.Parameters.Add("@d", SqlDbType.NVarChar, 500).Value = string.IsNullOrWhiteSpace(p.Descripcion) ? (object)DBNull.Value : p.Descripcion;
                                    id = Convert.ToInt32(cmd.ExecuteScalar());
                                }
                                if (p.Inicial > 0)
                                {
                                    long mov;
                                    using (var cmd = new SqlCommand(@"INSERT dbo.MOVIMIENTO_INVENTARIO
                                        (id_producto,id_usuario,tipo,cantidad,fecha,motivo,proveedor)
                                        OUTPUT INSERTED.id_movimiento VALUES(@id,@usuario,N'ENTRADA',@cantidad,SYSDATETIME(),N'Inventario inicial desde Excel',NULL)", cn, tx))
                                    {
                                        cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                                        cmd.Parameters.Add("@usuario", SqlDbType.Int).Value = usuario.Value;
                                        cmd.Parameters.Add("@cantidad", SqlDbType.Int).Value = p.Inicial;
                                        mov = Convert.ToInt64(cmd.ExecuteScalar());
                                    }
                                    int lote;
                                    using (var cmd = new SqlCommand(@"INSERT dbo.INVENTARIO_COCINA_LOTE
                                        (id_producto,cantidad_inicial,cantidad_disponible,fecha_vencimiento)
                                        OUTPUT INSERTED.id_lote VALUES(@id,@cantidad,@cantidad,@fecha)", cn, tx))
                                    {
                                        cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                                        cmd.Parameters.Add("@cantidad", SqlDbType.Int).Value = p.Inicial;
                                        cmd.Parameters.Add("@fecha", SqlDbType.Date).Value = p.Vencimiento.HasValue ? (object)p.Vencimiento.Value : DBNull.Value;
                                        lote = Convert.ToInt32(cmd.ExecuteScalar());
                                    }
                                    using (var cmd = new SqlCommand(@"INSERT dbo.INVENTARIO_COCINA_MOVIMIENTO_LOTE(id_movimiento,id_lote,cantidad)
                                        VALUES(@mov,@lote,@cantidad);
                                        UPDATE dbo.INVENTARIO SET stock_actual=@cantidad WHERE id_producto=@id", cn, tx))
                                    {
                                        cmd.Parameters.Add("@mov", SqlDbType.BigInt).Value = mov;
                                        cmd.Parameters.Add("@lote", SqlDbType.Int).Value = lote;
                                        cmd.Parameters.Add("@cantidad", SqlDbType.Int).Value = p.Inicial;
                                        cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
                TempData["Success"] = "Se importaron " + filas.Count + " productos de cocina.";
                return RedirectToAction("Index");
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is SqlException || ex is ArgumentException)
            {
                TempData["Error"] = "No se importaron productos. " + ex.Message;
                return RedirectToAction("Crear");
            }
        }
    }
}
