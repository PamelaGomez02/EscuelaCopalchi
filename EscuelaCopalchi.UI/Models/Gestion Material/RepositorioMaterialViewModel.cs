
using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace EscuelaCopalchi.UI.Models
{
    public class RepositorioMaterialViewModel
    {
        public int IdMaterial { get; set; }

        [Required(ErrorMessage = "Ingrese el título.")]
        [StringLength(200)]
        [Display(Name = "Título")]
        public string Titulo { get; set; }

        [StringLength(1000)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "Seleccione el tipo de material.")]
        [StringLength(50)]
        [Display(Name = "Tipo de material")]
        public string TipoMaterial { get; set; }

        [Required(ErrorMessage = "Seleccione una categoría.")]
        [StringLength(100)]
        [Display(Name = "Categoría")]
        public string Categoria { get; set; }

        [Required(ErrorMessage = "Seleccione el grado.")]
        [StringLength(50)]
        [Display(Name = "Grado")]
        public string Grado { get; set; }

        // Formato del archivo: PDF, Word, Excel, etc.
        [StringLength(20)]
        public string Tipo { get; set; }

        // Nombre del archivo almacenado
        [StringLength(500)]
        public string NombreArchivo { get; set; }

        [StringLength(1000)]
        public string Url { get; set; }

        public int? IdDocente { get; set; }

        public int? IdGrupo { get; set; }

        public int? IdUsuarioCreador { get; set; }

        public DateTime FechaCarga { get; set; }

        public bool Estado { get; set; }

        // Archivo recibido desde el formulario
        public HttpPostedFileBase Archivo { get; set; }

        // Propiedades auxiliares para la interfaz
        public string Extension { get; set; }

        public string TipoContenido { get; set; }

        public long TamanoBytes { get; set; }

        public DateTime? FechaModificacion { get; set; }

        public int Descargas { get; set; }

        public bool PuedeModificar { get; set; }

        public bool PuedeEditar { get; set; }

        public bool PuedeEliminar { get; set; }

        public bool PuedeDescargar { get; set; }

        public string NombreUsuarioCreador { get; set; }
    }
}
