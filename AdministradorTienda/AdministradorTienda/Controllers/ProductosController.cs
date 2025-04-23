using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AdministradorTienda.Data;
using AdministradorTienda.Models;
using AdministradorTienda.EmailSender;
using Microsoft.AspNetCore.Authorization;

namespace AdministradorTienda.Controllers
{
    [Authorize(Roles = "Administrador,Vendedor")]

    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICustomEmailSender _emailSender;

        public ProductosController(ApplicationDbContext context, ICustomEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // GET: Productos
        public async Task<IActionResult> Index(string FiltroPor, string ValorFiltro, decimal? PrecioMin, decimal? PrecioMax)
        {
            var productos = _context.Productos.Include(p => p.Categoria).AsQueryable();

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

            return View(await productos.ToListAsync());
        }

        // GET: Productos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(m => m.IdProducto == id);

            if (producto == null)
                return NotFound();

            return View(producto);
        }

        // GET: Productos/Create
        public IActionResult Create()
        {
            ViewData["IdCategoria"] = new SelectList(_context.Categorias, "IdCategoria", "Nombre");
            return View();
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdProducto,Nombre,Descripcion,Precio,Stock,IdCategoria")] Producto producto)
        {
            if (ModelState.IsValid)
            {
                _context.Add(producto);
                await _context.SaveChangesAsync();

                // Enviar correo cuando un nuevo producto sea agregado
                await EnviarCorreoNuevoProducto(producto);

                return RedirectToAction(nameof(Index));
            }

            ViewData["IdCategoria"] = new SelectList(_context.Categorias, "IdCategoria", "Nombre", producto.IdCategoria);
            return View(producto);
        }

        // Método para enviar correo sobre el nuevo producto
        private async Task EnviarCorreoNuevoProducto(Producto producto)
        {
            var emailSubject = $"Nuevo Producto Agregado: {producto.Nombre}";
            var emailBody = $"Hola Administrador,<br/><br/>Un nuevo producto ha sido agregado al sistema:<br/>" +
                            $"<strong>Nombre:</strong> {producto.Nombre}<br/>" +
                            $"<strong>Descripción:</strong> {producto.Descripcion}<br/>" +
                            $"<strong>Precio:</strong> {producto.Precio:C2}<br/>" +
                            $"<strong>Stock:</strong> {producto.Stock}<br/>" +
                            $"<strong>Categoría:</strong> {producto.Categoria?.Nombre ?? "No asignada"}<br/><br/>" +
                            "Gracias por tu atención.<br/>Farmacia Saule";

            // Puedes enviar el correo a una dirección específica, como un administrador
            await _emailSender.SendEmailAsync("admin@tienda.com", emailSubject, emailBody);
        }


        // GET: Productos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
                return NotFound();

            ViewData["IdCategoria"] = new SelectList(_context.Categorias, "IdCategoria", "Nombre", producto.IdCategoria);
            return View(producto);
        }

        // POST: Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdProducto,Nombre,Descripcion,Precio,Stock,IdCategoria")] Producto producto)
        {
            if (id != producto.IdProducto)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.IdProducto))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["IdCategoria"] = new SelectList(_context.Categorias, "IdCategoria", "Nombre", producto.IdCategoria);
            return View(producto);
        }

        // GET: Productos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(m => m.IdProducto == id);

            if (producto == null)
                return NotFound();

            return View(producto);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
                _context.Productos.Remove(producto);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.IdProducto == id);
        }
    }
}
