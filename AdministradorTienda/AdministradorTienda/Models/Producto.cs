namespace AdministradorTienda.Models
{
    public class Producto
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public int IdCategoria { get; set; }
        public Categoria? Categoria { get; set; }
        public ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();
    }
}
