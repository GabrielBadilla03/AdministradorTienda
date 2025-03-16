namespace AdministradorTienda.Models
{
    public class Pago
    {
        public int IdPago { get; set; }
        public int IdCliente { get; set; }
        public Cliente? Cliente { get; set; }
        public int IdPedido { get; set; }
        public Pedido? Pedido { get; set; }
        public string IdUsuarioRegistro { get; set; }
        public Usuario? UsuarioRegistro { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
        public string MetodoPago { get; set; }
    }
}
