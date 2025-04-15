using System;
using System.ComponentModel.DataAnnotations;

namespace AdministradorTienda.Models
{
    public class ReportePagos
    {
        [Display(Name = "ID Cliente")]
        public string ClienteId { get; set; }

        [Display(Name = "Nombre Cliente")]
        public string NombreCliente { get; set; }

        [Display(Name = "Total Pagado")]
        [DataType(DataType.Currency)]
        public decimal TotalPagado { get; set; }

        [Display(Name = "Último Pago")]
        [DataType(DataType.Date)]
        public DateTime UltimoPagoFecha { get; set; }

        [Display(Name = "Cantidad de Pagos")]
        public int CantidadPagos { get; set; }

        // Relación para los detalles
        public List<Pago> Pagos { get; set; } = new List<Pago>();
    }
}
