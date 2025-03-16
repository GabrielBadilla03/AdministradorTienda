using Microsoft.AspNetCore.Identity;

namespace AdministradorTienda.Models
{
    public class Usuario
    {
        public string IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public int Telefono { get; set; }
        public ICollection<Pedido> PedidosGestionados { get; set; } = new List<Pedido>();
        public ICollection<Pago> PagosRegistrados { get; set; } = new List<Pago>();
        public IdentityUser? AspNetUser { get; set; }

    }
}
