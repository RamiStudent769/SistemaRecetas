
namespace _20263RecetasApi.Models
{
    public class Receta
    {
        public int Id {  get; set; }
        public string Nombre { get; set;  } = string.Empty;
        public string? Descripcion { get; set; }
        public string Ingredientes { get; set; } = string.Empty;
        public string Preparacion { get ; set; } = string.Empty;
        public int CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }
    }
}
