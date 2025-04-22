using AdministradorTienda.Data;
using CasoPractico2.NoEmailSender;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
    
builder.Services.AddTransient<IEmailSender, NoEmailSender>();


var app = builder.Build();

app.MapGet("/api/ventas", async (ApplicationDbContext db, DateTime? startDate, DateTime? endDate, string? producto) =>
{
    var query = db.Pedidos.AsQueryable();

    if (startDate.HasValue)
        query = query.Where(v => v.FechaPedido >= startDate.Value);

    if (endDate.HasValue)
        query = query.Where(v => v.FechaPedido <= endDate.Value);

    if (!string.IsNullOrEmpty(producto))
        query = query.Where(v => v.DetallesPedido.Any(d => d.Producto.Nombre.Contains(producto)));

    var ventas = await query
        .Include(v => v.DetallesPedido)
        .ThenInclude(dp => dp.Producto)
        .ToListAsync();

    return Results.Ok(ventas);
});


app.MapGet("/api/productos", async (ApplicationDbContext db) =>
{
    var productos = await db.Productos
        .Include(p => p.Categoria)
        .Select(p => new
        {
            p.IdProducto,
            p.Nombre,
            p.Descripcion,
            p.Precio,
            p.Stock,
            CategoriaNombre = p.Categoria.Nombre
        })
        .ToListAsync();

    return Results.Ok(productos);
});

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = new[] { "Administrador", "Contador", "Repartidor", "Vendedor", "Usuario" };

    foreach (var role in roles)
    {
        var roleExists = roleManager.RoleExistsAsync(role).GetAwaiter().GetResult();
        if (!roleExists)
        {
            roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Habilitar Swagger
    app.UseSwaggerUI();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();