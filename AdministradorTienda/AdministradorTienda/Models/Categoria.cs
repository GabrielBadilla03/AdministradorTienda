namespace AdministradorTienda.Models
{
    public class Categoria
    {
        public int IdCategoria { get; set; }
        public string Nombre { get; set; }
        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
