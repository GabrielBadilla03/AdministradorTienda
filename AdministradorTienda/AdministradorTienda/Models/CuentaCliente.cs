namespace AdministradorTienda.Models
{
    public class CuentaCliente
    {
        public int IdCuenta { get; set; }
        public int IdCliente { get; set; }
        public Cliente? Cliente { get; set; }
        public decimal Saldo { get; set; }
    }
}
