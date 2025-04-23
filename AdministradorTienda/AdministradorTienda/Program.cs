// Licencia: 
// Copyright (c) [Año] [Nombre del titular de la licencia]
// Este código se distribuye bajo la Licencia [Nombre de la Licencia].
// Consulte el archivo LICENSE para más detalles.

using AdministradorTienda.Data;
using AdministradorTienda.EmailSender;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using Proyecto_FarmaScan.Service;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Por la siguiente línea:
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<ICustomEmailSender, EmailSender>();
builder.Services.AddScoped<IErrorService, ErrorService>();


var app = builder.Build();

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
