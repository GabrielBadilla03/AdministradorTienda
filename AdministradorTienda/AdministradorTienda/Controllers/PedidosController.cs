using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AdministradorTienda.Data;
using AdministradorTienda.Models;
using System.Security.Claims;
using QuestPDF.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Identity.UI.Services;
using AdministradorTienda.EmailSender;
using Microsoft.AspNetCore.Authorization;

namespace AdministradorTienda.Controllers
{
    [Authorize(Roles = "Administrador,Repartidor,Vendedor")]

    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICustomEmailSender _emailSender;
        public PedidosController(ApplicationDbContext context, ICustomEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // GET: Pedidos
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.UsuarioGestor);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Pedidos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.UsuarioGestor)
                .Include(p => p.DetallesPedido)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(m => m.IdPedido == id);

            if (pedido == null) return NotFound();

            ViewBag.TotalPedido = pedido.DetallesPedido.Sum(dp => dp.PrecioUnitario * dp.Cantidad);
            return View(pedido);
        }

        // GET: Pedidos/Create
        public IActionResult Create(string nombreCliente = null)
        {
            var clientesQuery = _context.Clientes.AsQueryable();

            if (!string.IsNullOrEmpty(nombreCliente))
            {
                clientesQuery = clientesQuery.Where(c => c.Nombre.Contains(nombreCliente));
                ViewBag.FiltroNombreCliente = nombreCliente;
            }

            var clientesList = clientesQuery
                .Select(c => new {
                    c.IdCliente,
                    NombreCompleto = c.Nombre + " " + c.Apellido
                }).ToList();

            ViewData["IdCliente"] = new SelectList(clientesList, "IdCliente", "NombreCompleto");

            var usuarios = _context.Usuarios
                .Select(u => new {
                    u.IdUsuario,
                    NombreCompleto = u.Nombre + " " + u.Apellido
                }).ToList();
            ViewData["IdUsuarioGestor"] = new SelectList(usuarios, "IdUsuario", "NombreCompleto");

            ViewBag.Estado = new SelectList(new List<string> { "Pendiente" });

            return View();
        }

        // POST: Pedidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int IdCliente, DateTime FechaPedido, string Estado)
        {
            if (ModelState.IsValid)
            {
                var aspNetUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(aspNetUserId)) return Unauthorized();

                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == aspNetUserId);
                if (usuario == null) return NotFound("Usuario no encontrado en la tabla Usuarios.");

                var pedido = new Pedido
                {
                    IdCliente = IdCliente,
                    FechaPedido = FechaPedido,
                    Estado = Estado,
                    IdUsuarioGestor = usuario.IdUsuario
                };

                _context.Add(pedido);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var clientes = _context.Clientes
                .Select(c => new {
                    c.IdCliente,
                    NombreCompleto = c.Nombre + " " + c.Apellido
                }).ToList();
            ViewData["IdCliente"] = new SelectList(clientes, "IdCliente", "NombreCompleto", IdCliente);

            var usuarios = _context.Usuarios
                .Select(u => new {
                    u.IdUsuario,
                    NombreCompleto = u.Nombre + " " + u.Apellido
                }).ToList();
            ViewData["IdUsuarioGestor"] = new SelectList(usuarios, "IdUsuario", "NombreCompleto");

            ViewBag.Estado = new SelectList(new List<string> { "Pendiente" });

            return View();
        }

        // GET: Pedidos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            var clientes = _context.Clientes
                .Select(c => new {
                    c.IdCliente,
                    NombreCompleto = c.Nombre + " " + c.Apellido
                }).ToList();
            ViewData["IdCliente"] = new SelectList(clientes, "IdCliente", "NombreCompleto", pedido.IdCliente);

            var usuarios = _context.Usuarios
                .Select(u => new {
                    u.IdUsuario,
                    NombreCompleto = u.Nombre + " " + u.Apellido
                }).ToList();
            ViewData["IdUsuarioGestor"] = new SelectList(usuarios, "IdUsuario", "NombreCompleto", pedido.IdUsuarioGestor);

            ViewBag.Estado = new SelectList(new[] { "Pendiente", "Entregado", "Cancelado", "Pagado" }, pedido.Estado);

            return View(pedido);
        }

        // POST: Pedidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DateTime fechaPedido, string estado, decimal? descuento)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.DetallesPedido)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.IdPedido == id);

            if (pedido == null) return NotFound();

            pedido.FechaPedido = fechaPedido;
            pedido.Estado = estado;

            var cuentaCliente = await _context.CuentasClientes
                .FirstOrDefaultAsync(cc => cc.IdCliente == pedido.IdCliente);

            if (cuentaCliente == null)
            {
                ModelState.AddModelError("", "El cliente no tiene una cuenta creada.");
                return View(pedido);
            }

            decimal total = pedido.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario);

            switch (estado)
            {
                case "Pagado":
                    if (descuento.HasValue)
                    {
                        decimal porcentaje = descuento.Value / 100;
                        total -= total * porcentaje;
                    }

                    if (cuentaCliente.Saldo + total < 0)
                    {
                        ModelState.AddModelError("", "El saldo del cliente no puede quedar en negativo.");
                        return View(pedido);
                    }

                    cuentaCliente.Saldo += total;

                    // Registrar el pago automático
                    var pago = new Pago
                    {
                        IdCliente = pedido.IdCliente,
                        IdPedido = pedido.IdPedido,
                        IdUsuarioRegistro = pedido.IdUsuarioGestor,
                        Monto = total,
                        FechaPago = DateTime.Now,
                        MetodoPago = "Contado"
                    };
                    _context.Pagos.Add(pago);

                    // Enviar correo cuando el estado sea "Pagado"
                    await EnviarCorreoEstadoCambio(pedido, "Pagado");
                    break;

                case "Entregado":
                    if (cuentaCliente.Saldo + total < 0)
                    {
                        ModelState.AddModelError("", "El saldo del cliente no puede quedar en negativo.");
                        return View(pedido);
                    }

                    cuentaCliente.Saldo += total;

                    // Enviar correo cuando el estado sea "Entregado"
                    await EnviarCorreoEstadoCambio(pedido, "Entregado");
                    break;

                case "Pendiente":
                    foreach (var detalle in pedido.DetallesPedido)
                    {
                        if (detalle.Producto.Stock < detalle.Cantidad)
                        {
                            ModelState.AddModelError("", $"No hay suficiente stock para el producto {detalle.Producto.Nombre}");
                            return View(pedido);
                        }
                    }

                    foreach (var detalle in pedido.DetallesPedido)
                    {
                        detalle.Producto.Stock -= detalle.Cantidad;
                        _context.Update(detalle.Producto);
                    }
                    break;

                case "Cancelado":
                    foreach (var detalle in pedido.DetallesPedido)
                    {
                        detalle.Producto.Stock += detalle.Cantidad;
                        _context.Update(detalle.Producto);
                    }
                    break;
            }

            try
            {
                _context.Update(pedido);
                _context.Update(cuentaCliente);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PedidoExists(id)) return NotFound();
                else throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // Método para enviar correo dependiendo del estado
        private async Task EnviarCorreoEstadoCambio(Pedido pedido, string estado)
        {
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.IdCliente == pedido.IdCliente);

            if (cliente == null)
                return;

            var emailSubject = $"Estado del pedido {pedido.IdPedido} - {estado}";
            var emailBody = $"Hola {cliente.Nombre},<br/><br/>Tu pedido con ID {pedido.IdPedido} ha sido marcado como {estado}.<br/>" +
                            $"El monto total es: {pedido.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario):C2}.<br/><br/>" +
                            "Gracias por tu compra.<br/>Farmacia Saule";

            await _emailSender.SendEmailAsync(cliente.Email, emailSubject, emailBody);
        }


        // GET: Pedidos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.UsuarioGestor)
                .FirstOrDefaultAsync(m => m.IdPedido == id);
            if (pedido == null) return NotFound();

            return View(pedido);
        }

        // POST: Pedidos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                _context.Pedidos.Remove(pedido);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PedidoExists(int id)
        {
            return _context.Pedidos.Any(e => e.IdPedido == id);
        }

        public async Task<IActionResult> DescargarFactura(int id)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.UsuarioGestor)
                .Include(p => p.DetallesPedido)
                    .ThenInclude(dp => dp.Producto)
                .FirstOrDefaultAsync(p => p.IdPedido == id);

            if (pedido == null) return NotFound();

            decimal total = pedido.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario);
            var stream = new MemoryStream();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Factura - Pedido #{pedido.IdPedido}").FontSize(20).Bold();
                        col.Item().Text($"Cliente: {pedido.Cliente.Nombre}");
                        col.Item().Text($"Gestor: {pedido.UsuarioGestor.Nombre}");
                        col.Item().Text($"Fecha: {pedido.FechaPedido:dd/MM/yyyy}");
                        col.Item().Text($"Estado: {pedido.Estado}");
                        col.Item().LineHorizontal(1).LineColor(Colors.Black);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(100);
                                columns.RelativeColumn();
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Código").Bold();
                                header.Cell().Text("Producto").Bold();
                                header.Cell().Text("Cantidad").Bold();
                                header.Cell().Text("Precio").Bold();
                            });

                            foreach (var detalle in pedido.DetallesPedido)
                            {
                                table.Cell().Text(detalle.Producto.IdProducto);
                                table.Cell().Text(detalle.Producto.Nombre);
                                table.Cell().Text(detalle.Cantidad.ToString());
                                table.Cell().Text($"{detalle.PrecioUnitario:C}");
                            }
                        });

                        col.Item().LineHorizontal(1).LineColor(Colors.Black);
                        col.Item().Text($"Total: {total:C}").FontSize(16).Bold().AlignRight();
                    });
                });
            });

            document.GeneratePdf(stream);
            stream.Position = 0;
            return File(stream, "application/pdf", $"Factura_Pedido_{pedido.IdPedido}.pdf");
        }
    }
}
