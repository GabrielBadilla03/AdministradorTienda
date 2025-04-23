using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AdministradorTienda.Data;
using AdministradorTienda.Models;
using Microsoft.AspNetCore.Authorization;

namespace AdministradorTienda.Controllers
{
    [Authorize(Roles = "Administrador,Repartidor,Vendedor")]

    public class DetallePedidosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DetallePedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DetallePedidos
        public async Task<IActionResult> Index(int? idPedido)
        {
            var detalles = _context.DetallesPedido
                .Include(d => d.Pedido)
                .Include(d => d.Producto)
                .AsQueryable();

            if (idPedido.HasValue)
            {
                detalles = detalles.Where(d => d.IdPedido == idPedido.Value);
                ViewBag.FiltroIdPedido = idPedido.Value; // para mostrar en la vista si se desea
            }

            return View(await detalles.ToListAsync());
        }

        // GET: DetallePedidos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detallePedido = await _context.DetallesPedido
                .Include(d => d.Pedido)
                .Include(d => d.Producto)
                .FirstOrDefaultAsync(m => m.IdDetalle == id);
            if (detallePedido == null)
            {
                return NotFound();
            }

            return View(detallePedido);
        }

        // GET: DetallePedidos/Create
        public IActionResult Create(int? idPedido, string FiltroPor, string ValorFiltro, decimal? PrecioMin, decimal? PrecioMax)
        {
            if (idPedido != null)
            {
                ViewData["IdPedido"] = new SelectList(_context.Pedidos.Where(p => p.IdPedido == idPedido), "IdPedido", "IdPedido");
                ViewBag.IdPedidoPreseleccionado = idPedido;
            }
            else
            {
                ViewData["IdPedido"] = new SelectList(_context.Pedidos, "IdPedido", "IdPedido");
            }

            var productos = _context.Productos.Include(p => p.Categoria).AsQueryable();

            bool hayFiltros = !string.IsNullOrEmpty(FiltroPor) ||
                              !string.IsNullOrEmpty(ValorFiltro) ||
                              PrecioMin.HasValue ||
                              PrecioMax.HasValue;

            if (hayFiltros)
            {
                if (!string.IsNullOrEmpty(FiltroPor))
                {
                    switch (FiltroPor)
                    {
                        case "Nombre":
                            if (!string.IsNullOrEmpty(ValorFiltro))
                                productos = productos.Where(p => p.Nombre.Contains(ValorFiltro));
                            break;
                        case "Categoria":
                            if (!string.IsNullOrEmpty(ValorFiltro))
                                productos = productos.Where(p => p.Categoria.Nombre.Contains(ValorFiltro));
                            break;
                        case "Precio":
                            if (PrecioMin.HasValue)
                                productos = productos.Where(p => p.Precio >= PrecioMin.Value);
                            if (PrecioMax.HasValue)
                                productos = productos.Where(p => p.Precio <= PrecioMax.Value);
                            break;
                    }
                }
            }

            var productosFiltrados = productos.ToList(); // Se ejecuta la consulta con o sin filtros

            ViewData["IdProducto"] = new SelectList(productosFiltrados, "IdProducto", "Nombre");

            ViewBag.ProductosConPrecio = productosFiltrados
                .Select(p => new { p.IdProducto, p.Precio })
                .ToDictionary(p => p.IdProducto, p => p.Precio);

            return View();
        }

        // POST: DetallePedidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdDetalle,IdPedido,IdProducto,Cantidad,PrecioUnitario")] DetallePedido detallePedido)
        {
            // Obtener el producto correspondiente
            var producto = await _context.Productos.FindAsync(detallePedido.IdProducto);

            if (producto == null)
            {
                ModelState.AddModelError("IdProducto", "Producto no encontrado.");
            }
            else if (detallePedido.Cantidad > producto.Stock)
            {
                ModelState.AddModelError("Cantidad", $"La cantidad solicitada ({detallePedido.Cantidad}) supera el stock disponible ({producto.Stock}).");
            }

            if (ModelState.IsValid)
            {
                _context.Add(detallePedido);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["IdPedido"] = new SelectList(_context.Pedidos, "IdPedido", "IdPedido", detallePedido.IdPedido);
            ViewData["IdProducto"] = new SelectList(_context.Productos, "IdProducto", "Nombre", detallePedido.IdProducto);

            return View(detallePedido); // Volvemos a la vista con errores si los hay
        }

        // GET: DetallePedidos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detallePedido = await _context.DetallesPedido.FindAsync(id);
            if (detallePedido == null)
            {
                return NotFound();
            }
            ViewData["IdPedido"] = new SelectList(_context.Pedidos, "IdPedido", "IdPedido", detallePedido.IdPedido);
            ViewData["IdProducto"] = new SelectList(_context.Productos, "IdProducto", "Nombre", detallePedido.IdProducto);
            return View(detallePedido);
        }

        // POST: DetallePedidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdDetalle,IdPedido,IdProducto,Cantidad,PrecioUnitario")] DetallePedido detallePedido)
        {
            if (id != detallePedido.IdDetalle)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(detallePedido);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DetallePedidoExists(detallePedido.IdDetalle))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdPedido"] = new SelectList(_context.Pedidos, "IdPedido", "IdPedido", detallePedido.IdPedido);
            ViewData["IdProducto"] = new SelectList(_context.Productos, "IdProducto", "Nombre", detallePedido.IdProducto);
            return View(detallePedido);
        }

        // GET: DetallePedidos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detallePedido = await _context.DetallesPedido
                .Include(d => d.Pedido)
                .Include(d => d.Producto)
                .FirstOrDefaultAsync(m => m.IdDetalle == id);
            if (detallePedido == null)
            {
                return NotFound();
            }

            return View(detallePedido);
        }

        // POST: DetallePedidos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detallePedido = await _context.DetallesPedido.FindAsync(id);
            if (detallePedido != null)
            {
                _context.DetallesPedido.Remove(detallePedido);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DetallePedidoExists(int id)
        {
            return _context.DetallesPedido.Any(e => e.IdDetalle == id);
        }
    }
}
