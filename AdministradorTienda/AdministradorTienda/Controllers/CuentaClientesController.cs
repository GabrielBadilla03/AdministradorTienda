using System;
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
    [Authorize(Roles = "Administrador,Contador")]

    public class CuentaClientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CuentaClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CuentaClientes
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.CuentasClientes.Include(c => c.Cliente);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: CuentaClientes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cuentaCliente = await _context.CuentasClientes
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(m => m.IdCuenta == id);
            if (cuentaCliente == null)
            {
                return NotFound();
            }

            return View(cuentaCliente);
        }

        // GET: CuentaClientes/Create
        public IActionResult Create()
        {
            // Crear una lista de clientes con el nombre completo
            ViewData["IdCliente"] = new SelectList(
                _context.Clientes
                    .Select(c => new { c.IdCliente, NombreCompleto = c.Nombre + " " + c.Apellido })
                    .ToList(),
                "IdCliente",
                "NombreCompleto"
            );
            return View();
        }

        // POST: CuentaClientes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdCuenta,IdCliente,Saldo")] CuentaCliente cuentaCliente)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cuentaCliente);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // Crear una lista de clientes con el nombre completo
            ViewData["IdCliente"] = new SelectList(
                _context.Clientes
                    .Select(c => new { c.IdCliente, NombreCompleto = c.Nombre + " " + c.Apellido })
                    .ToList(),
                "IdCliente",
                "NombreCompleto",
                cuentaCliente.IdCliente
            );
            return View(cuentaCliente);
        }

        // GET: CuentaClientes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cuentaCliente = await _context.CuentasClientes.FindAsync(id);
            if (cuentaCliente == null)
            {
                return NotFound();
            }
            // Crear una lista de clientes con el nombre completo
            ViewData["IdCliente"] = new SelectList(
                _context.Clientes
                    .Select(c => new { c.IdCliente, NombreCompleto = c.Nombre + " " + c.Apellido })
                    .ToList(),
                "IdCliente",
                "NombreCompleto",
                cuentaCliente.IdCliente
            );
            return View(cuentaCliente);
        }

        // POST: CuentaClientes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdCuenta,IdCliente,Saldo")] CuentaCliente cuentaCliente)
        {
            if (id != cuentaCliente.IdCuenta)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cuentaCliente);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CuentaClienteExists(cuentaCliente.IdCuenta))
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
            // Crear una lista de clientes con el nombre completo
            ViewData["IdCliente"] = new SelectList(
                _context.Clientes
                    .Select(c => new { c.IdCliente, NombreCompleto = c.Nombre + " " + c.Apellido })
                    .ToList(),
                "IdCliente",
                "NombreCompleto",
                cuentaCliente.IdCliente
            );
            return View(cuentaCliente);
        }

        // GET: CuentaClientes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cuentaCliente = await _context.CuentasClientes
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(m => m.IdCuenta == id);
            if (cuentaCliente == null)
            {
                return NotFound();
            }

            return View(cuentaCliente);
        }

        // POST: CuentaClientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cuentaCliente = await _context.CuentasClientes.FindAsync(id);
            if (cuentaCliente != null)
            {
                _context.CuentasClientes.Remove(cuentaCliente);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CuentaClienteExists(int id)
        {
            return _context.CuentasClientes.Any(e => e.IdCuenta == id);
        }
    }
}
