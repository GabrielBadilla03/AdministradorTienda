using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AdministradorTienda.Data;
using AdministradorTienda.Models;

namespace AdministradorTienda.Controllers
{
    public class PagosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PagosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Pagos
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Pagos.Include(p => p.Cliente).Include(p => p.Pedido).Include(p => p.UsuarioRegistro);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Pagos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pago = await _context.Pagos
                .Include(p => p.Cliente)
                .Include(p => p.Pedido)
                .Include(p => p.UsuarioRegistro)
                .FirstOrDefaultAsync(m => m.IdPago == id);
            if (pago == null)
            {
                return NotFound();
            }

            return View(pago);
        }

        // GET: Pagos/Create
        public IActionResult Create()
        {
            ViewData["IdCliente"] = new SelectList(_context.Clientes, "IdCliente", "IdCliente");
            ViewData["IdPedido"] = new SelectList(_context.Pedidos, "IdPedido", "IdPedido");
            ViewData["IdUsuarioRegistro"] = new SelectList(_context.Usuarios, "IdUsuario", "IdUsuario");
            return View();
        }

        // POST: Pagos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdPago,IdCliente,IdPedido,IdUsuarioRegistro,Monto,FechaPago,MetodoPago")] Pago pago)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pago);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdCliente"] = new SelectList(_context.Clientes, "IdCliente", "IdCliente", pago.IdCliente);
            ViewData["IdPedido"] = new SelectList(_context.Pedidos, "IdPedido", "IdPedido", pago.IdPedido);
            ViewData["IdUsuarioRegistro"] = new SelectList(_context.Usuarios, "IdUsuario", "IdUsuario", pago.IdUsuarioRegistro);
            return View(pago);
        }

        // GET: Pagos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pago = await _context.Pagos.FindAsync(id);
            if (pago == null)
            {
                return NotFound();
            }
            ViewData["IdCliente"] = new SelectList(_context.Clientes, "IdCliente", "IdCliente", pago.IdCliente);
            ViewData["IdPedido"] = new SelectList(_context.Pedidos, "IdPedido", "IdPedido", pago.IdPedido);
            ViewData["IdUsuarioRegistro"] = new SelectList(_context.Usuarios, "IdUsuario", "IdUsuario", pago.IdUsuarioRegistro);
            return View(pago);
        }

        // POST: Pagos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPago,IdCliente,IdPedido,IdUsuarioRegistro,Monto,FechaPago,MetodoPago")] Pago pago)
        {
            if (id != pago.IdPago)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pago);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PagoExists(pago.IdPago))
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
            ViewData["IdCliente"] = new SelectList(_context.Clientes, "IdCliente", "IdCliente", pago.IdCliente);
            ViewData["IdPedido"] = new SelectList(_context.Pedidos, "IdPedido", "IdPedido", pago.IdPedido);
            ViewData["IdUsuarioRegistro"] = new SelectList(_context.Usuarios, "IdUsuario", "IdUsuario", pago.IdUsuarioRegistro);
            return View(pago);
        }

        // GET: Pagos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pago = await _context.Pagos
                .Include(p => p.Cliente)
                .Include(p => p.Pedido)
                .Include(p => p.UsuarioRegistro)
                .FirstOrDefaultAsync(m => m.IdPago == id);
            if (pago == null)
            {
                return NotFound();
            }

            return View(pago);
        }

        // POST: Pagos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pago = await _context.Pagos.FindAsync(id);
            if (pago != null)
            {
                _context.Pagos.Remove(pago);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PagoExists(int id)
        {
            return _context.Pagos.Any(e => e.IdPago == id);
        }
    }
}
