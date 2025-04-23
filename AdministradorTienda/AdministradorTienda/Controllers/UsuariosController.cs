using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AdministradorTienda.Data;
using AdministradorTienda.Models;
using Microsoft.AspNetCore.Identity;

namespace AdministradorTienda.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index(string filtro)
        {
            var usuarios = _context.Usuarios.Include(u => u.AspNetUser).AsQueryable();

            if (!string.IsNullOrEmpty(filtro))
            {
                usuarios = usuarios.Where(u =>
                    u.Nombre.Contains(filtro) ||
                    u.Apellido.Contains(filtro) ||
                    u.Email.Contains(filtro));
            }

            return View(await usuarios.ToListAsync());
        }

        // GET: Usuarios/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .Include(u => u.AspNetUser)
                .FirstOrDefaultAsync(m => m.IdUsuario == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // GET: Usuarios/Create
        public IActionResult Create()
        {
            ViewData["IdUsuario"] = new SelectList(
                _context.Users,
                "Id",
                "FullName",
                null
            );
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdUsuario,Nombre,Apellido,Email,Telefono")] Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                _context.Add(usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["IdUsuario"] = new SelectList(
                _context.Users,
                "Id",
                "FullName",
                usuario.IdUsuario
            );
            return View(usuario);
        }

        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .Include(u => u.AspNetUser) // Asegura que se incluya la relación con IdentityUser
                .FirstOrDefaultAsync(u => u.IdUsuario == id);
            if (usuario == null) return NotFound();

            // Obtener los roles asignados al usuario
            var rolesAsignadosIds = await _context.UserRoles
                .Where(ur => ur.UserId == usuario.AspNetUser.Id)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            var rolesAsignados = await _context.Roles
                .Where(r => rolesAsignadosIds.Contains(r.Id))
                .ToListAsync();

            var rolesNoAsignados = await _context.Roles
                .Where(r => !rolesAsignadosIds.Contains(r.Id))
                .ToListAsync();

            // Asignar los roles al ViewData
            ViewData["RolesAsignados"] = new SelectList(rolesAsignados, "Id", "Name");
            ViewData["RolesNoAsignados"] = new SelectList(rolesNoAsignados, "Id", "Name");

            return View(usuario);
        }


        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            string Nombre,
            string Apellido,
            string Email,
            int Telefono,
            string? RolAsignar,
            string?  RolNoAsignar)
        {
            if (id == null) return NotFound();

            var usuarioExistente = await _context.Usuarios.FindAsync(id);
            var usuarioAspNet = await _context.Users.FindAsync(id);

            if (usuarioExistente == null || usuarioAspNet == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Actualizar datos básicos
                    usuarioExistente.Nombre = Nombre;
                    usuarioExistente.Apellido = Apellido;
                    usuarioExistente.Email = Email;
                    usuarioExistente.Telefono = Telefono;

                    // Asignar nuevo rol si viene uno
                    if (!string.IsNullOrEmpty(RolAsignar))
                    {
                        bool yaTieneRol = await _context.UserRoles.AnyAsync(ur =>
                            ur.UserId == usuarioAspNet.Id && ur.RoleId == RolAsignar);

                        if (!yaTieneRol)
                        {
                            var nuevoRol = new IdentityUserRole<string>
                            {
                                UserId = usuarioAspNet.Id,
                                RoleId = RolAsignar
                            };
                            _context.UserRoles.Add(nuevoRol);
                        }
                    }

                    // Quitar rol si viene uno
                    if (!string.IsNullOrEmpty(RolNoAsignar))
                    {
                        var rolAEliminar = await _context.UserRoles.FirstOrDefaultAsync(ur =>
                            ur.UserId == usuarioAspNet.Id && ur.RoleId == RolNoAsignar);

                        if (rolAEliminar != null)
                        {
                            _context.UserRoles.Remove(rolAEliminar);
                        }
                    }

                    // Guardar cambios
                    _context.Update(usuarioExistente);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(id)) return NotFound();
                    else throw;
                }
            }

            // Recargar roles si algo falla
            var rolesAsignadosIds = await _context.UserRoles
                .Where(ur => ur.UserId == usuarioAspNet.Id)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            var rolesAsignados = await _context.Roles
                .Where(r => rolesAsignadosIds.Contains(r.Id))
                .ToListAsync();

            var rolesNoAsignados = await _context.Roles
                .Where(r => !rolesAsignadosIds.Contains(r.Id))
                .ToListAsync();

            ViewData["RolesAsignados"] = new SelectList(rolesAsignados, "Id", "Name");
            ViewData["RolesNoAsignados"] = new SelectList(rolesNoAsignados, "Id", "Name");

            return View(usuarioExistente);
        }





        // GET: Usuarios/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .Include(u => u.AspNetUser)
                .FirstOrDefaultAsync(m => m.IdUsuario == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UsuarioExists(string id)
        {
            return _context.Usuarios.Any(e => e.IdUsuario == id);
        }
    }
}
