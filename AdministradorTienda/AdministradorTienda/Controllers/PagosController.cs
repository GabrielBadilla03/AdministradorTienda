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
    [Authorize(Roles = "Administrador,Contador")]

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(decimal Monto, int IdCliente, string MetodoPago)
        {
            if (ModelState.IsValid)
            {
                // Obtener el usuario logueado
                var aspNetUserId = _context.Users
                    .Where(u => u.UserName == User.Identity.Name)
                    .Select(u => u.Id)
                    .FirstOrDefault();

                if (aspNetUserId == null)
                {
                    ModelState.AddModelError("", "No se pudo identificar el usuario logueado.");
                    return View();
                }

                // Buscar en tabla Usuarios el que tiene ese IdAspNetUser
                var usuarioRegistro = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.IdUsuario == aspNetUserId);

                if (usuarioRegistro == null)
                {
                    ModelState.AddModelError("", "No se encontró el usuario registrado en la base de datos.");
                    return View();
                }

                // Obtener el pedido más reciente del cliente
                var pedidoReciente = await _context.Pedidos
                    .Where(p => p.IdCliente == IdCliente)
                    .OrderByDescending(p => p.FechaPedido)
                    .FirstOrDefaultAsync();

                if (pedidoReciente == null)
                {
                    ModelState.AddModelError("", "El cliente no tiene pedidos registrados.");
                    return View();
                }

                // Crear el objeto de pago
                var pago = new Pago
                {
                    IdCliente = IdCliente,
                    IdPedido = pedidoReciente.IdPedido,
                    IdUsuarioRegistro = usuarioRegistro.IdUsuario,
                    FechaPago = DateTime.Now,
                    Monto = Monto,
                    MetodoPago = MetodoPago
                };

                _context.Pagos.Add(pago);

                // Descontar saldo de cuenta del cliente
                var cuentaCliente = await _context.CuentasClientes
                    .FirstOrDefaultAsync(c => c.IdCliente == IdCliente);

                if (cuentaCliente != null)
                {
                    cuentaCliente.Saldo -= Monto;

                    if (cuentaCliente.Saldo < 0)
                    {
                        ModelState.AddModelError("", "El pago excede el saldo disponible del cliente.");
                        return View();
                    }

                    _context.Update(cuentaCliente);
                }
                else
                {
                    // Si no hay cuenta, la creamos
                    var nuevaCuenta = new CuentaCliente
                    {
                        IdCliente = IdCliente,
                        Saldo = -Monto // Si se crea una nueva cuenta, asigna el monto del pago como saldo negativo
                    };

                    _context.CuentasClientes.Add(nuevaCuenta);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si algo falla, recargar el ViewData del cliente
            ViewData["IdCliente"] = new SelectList(
                _context.Clientes.Select(c => new { c.IdCliente, NombreCompleto = c.Nombre + " " + c.Apellido }).ToList(),
                "IdCliente",
                "NombreCompleto",
                IdCliente
            );

            return View();
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

            return View(pago);
        }



        // POST: Pagos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string metodoPago)
        {
            // Verificar si el pago existe
            var pago = await _context.Pagos.FindAsync(id);
            if (pago == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Actualizar solo el MetodoPago
                    pago.MetodoPago = metodoPago;

                    // Guardar cambios
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

            return View(pago);
        }



        private bool PedidoExists(int idPedido)
        {
            throw new NotImplementedException();
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
