using AdministradorTienda.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AdministradorTienda.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedido { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<CuentaCliente> CuentasClientes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>().HasKey(u => u.IdUsuario);
            modelBuilder.Entity<Cliente>().HasKey(c => c.IdCliente);
            modelBuilder.Entity<Categoria>().HasKey(c => c.IdCategoria);
            modelBuilder.Entity<Producto>().HasKey(p => p.IdProducto);
            modelBuilder.Entity<Pedido>().HasKey(p => p.IdPedido);
            modelBuilder.Entity<DetallePedido>().HasKey(dp => dp.IdDetalle);
            modelBuilder.Entity<Pago>().HasKey(p => p.IdPago);
            modelBuilder.Entity<CuentaCliente>().HasKey(cc => cc.IdCuenta);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Cliente)
                .WithMany(c => c.Pedidos)
                .HasForeignKey(p => p.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);  


            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.UsuarioGestor)
                .WithMany(u => u.PedidosGestionados)
                .HasForeignKey(p => p.IdUsuarioGestor)
                .OnDelete(DeleteBehavior.Restrict);  


            modelBuilder.Entity<DetallePedido>()
                .HasOne(dp => dp.Pedido)
                .WithMany(p => p.DetallesPedido)
                .HasForeignKey(dp => dp.IdPedido)
                .OnDelete(DeleteBehavior.Cascade);  

            modelBuilder.Entity<DetallePedido>()
                .HasOne(dp => dp.Producto)
                .WithMany(pr => pr.DetallesPedido)
                .HasForeignKey(dp => dp.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);  

            modelBuilder.Entity<Pago>()
                .HasOne(p => p.Cliente)
                .WithMany(c => c.Pagos)
                .HasForeignKey(p => p.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);  


            modelBuilder.Entity<Pago>()
                .HasOne(p => p.Pedido)
                .WithMany(p => p.Pagos)
                .HasForeignKey(p => p.IdPedido)
                .OnDelete(DeleteBehavior.Restrict);  


            modelBuilder.Entity<Pago>()
                .HasOne(p => p.UsuarioRegistro)
                .WithMany(u => u.PagosRegistrados)
                .HasForeignKey(p => p.IdUsuarioRegistro)
                .OnDelete(DeleteBehavior.Restrict); 


            modelBuilder.Entity<CuentaCliente>()
                .HasOne(cc => cc.Cliente)
                .WithOne(c => c.CuentaCliente)
                .HasForeignKey<CuentaCliente>(cc => cc.IdCliente)
                .OnDelete(DeleteBehavior.Cascade); 


            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.IdCategoria)
                .OnDelete(DeleteBehavior.Cascade);  


            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.AspNetUser)
                .WithOne()
                .HasForeignKey<Usuario>(u => u.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Categoria>()
                .HasKey(c => c.IdCategoria);
        }
    }
}
