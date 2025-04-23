using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdministradorTienda.Data;
using AdministradorTienda.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace AdministradorTienda.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AlertasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int STOCK_MINIMO = 5; // Valor configurable

        public AlertasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Alertas
        public async Task<IActionResult> Index()
        {
            var productosAlerta = await ObtenerProductosStockBajoAsync();
            ViewBag.StockMinimo = STOCK_MINIMO;
            return View(productosAlerta);
        }

        private async Task<List<Producto>> ObtenerProductosStockBajoAsync()
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Stock < STOCK_MINIMO)
                .OrderBy(p => p.Stock)
                .ToListAsync();
        }
    }
}