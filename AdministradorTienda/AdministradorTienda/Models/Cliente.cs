namespace AdministradorTienda.Models
{
    public class Cliente
    {
        public int IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
        public CuentaCliente? CuentaCliente { get; set; }
    }
}
