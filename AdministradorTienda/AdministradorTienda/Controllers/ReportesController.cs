using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdministradorTienda.Data;
using AdministradorTienda.Models;
using System.Linq;
using System.Threading.Tasks;

namespace AdministradorTienda.Controllers
{
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reportes/PagosClientes
        public async Task<IActionResult> Index()
        {
            var reporte = await _context.Pagos
                .Include(p => p.Cliente)
                .GroupBy(p => new { p.Cliente.IdCliente, p.Cliente.Nombre, p.Cliente.Apellido })
                .Select(g => new ReportePagos
                {
                    ClienteId = g.Key.IdCliente.ToString(),
                    NombreCliente = $"{g.Key.Nombre} {g.Key.Apellido}",
                    TotalPagado = g.Sum(p => p.Monto),
                    UltimoPagoFecha = g.Max(p => p.FechaPago),
                    CantidadPagos = g.Count(),
                    Pagos = g.ToList()
                })
                .OrderByDescending(r => r.TotalPagado)
                .ToListAsync();

            return View(reporte);
        }

        // GET: Reportes/DetallePagos/{clienteId}
        public async Task<IActionResult> Details(int clienteId)
        {
            var pagos = await _context.Pagos
                .Include(p => p.Cliente)
                .Where(p => p.Cliente.IdCliente == clienteId)
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();

            if (!pagos.Any())
            {
                return NotFound();
            }

            var cliente = pagos.First().Cliente;
            ViewBag.NombreCliente = $"{cliente.Nombre} {cliente.Apellido}";

            return View(pagos);
        }
    }
}