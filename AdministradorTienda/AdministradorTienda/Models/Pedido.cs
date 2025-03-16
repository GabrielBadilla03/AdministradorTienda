namespace AdministradorTienda.Models
{
    public class Pedido
    {
        public int IdPedido { get; set; }
        public int IdCliente { get; set; }
        public Cliente? Cliente { get; set; }
        public string IdUsuarioGestor { get; set; }
        public Usuario? UsuarioGestor { get; set; }
        public DateTime FechaPedido { get; set; }
        public string Estado { get; set; }
        public ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }
}
