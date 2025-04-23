using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdministradorTienda.Data;
using AdministradorTienda.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;
using SkiaSharp;
using QuestPDF;
using Microsoft.AspNetCore.Authorization;

namespace AdministradorTienda.Controllers
{
    [Authorize(Roles = "Administrador")]

    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var reporte = await _context.Pagos
                .Include(p => p.Cliente)
                .GroupBy(p => new { p.Cliente.IdCliente, p.Cliente.Nombre, p.Cliente.Apellido })
                .Select(g => new ReportePagos
                {
                    ClienteId = g.Key.IdCliente.ToString(),
                    NombreCliente = $"{g.Key.Nombre} {g.Key.Apellido}",
                    TotalPagado = g.Sum(p => p.Monto),
                    UltimoPagoFecha = g.Max(p => p.FechaPago),
                    CantidadPagos = g.Count(),
                    Pagos = g.ToList()
                })
                .OrderByDescending(r => r.TotalPagado)
                .ToListAsync();

            return View(reporte);
        }

        public async Task<IActionResult> Details(int clienteId)
        {
            var pagos = await _context.Pagos
                .Include(p => p.Cliente)
                .Where(p => p.Cliente.IdCliente == clienteId)
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();

            if (!pagos.Any())
                return NotFound();

            var cliente = pagos.First().Cliente;
            ViewBag.NombreCliente = $"{cliente.Nombre} {cliente.Apellido}";

            return View(pagos);
        }

        [HttpPost]
        public async Task<IActionResult> GenerarReporte(string tipo, DateTime fecha, string formato)
        {
            DateTime inicio, fin;
            switch (tipo)
            {
                case "semana":
                    int diff = (7 + (fecha.DayOfWeek - DayOfWeek.Monday)) % 7;
                    inicio = fecha.AddDays(-diff).Date;
                    fin = inicio.AddDays(6);
                    break;
                case "mes":
                    inicio = new DateTime(fecha.Year, fecha.Month, 1);
                    fin = inicio.AddMonths(1).AddDays(-1);
                    break;
                default:
                    inicio = fecha.Date;
                    fin = fecha.Date;
                    break;
            }
            var pagos = await _context.Pagos
                .Include(p => p.Pedido)
                    .ThenInclude(pe => pe.DetallesPedido)
                        .ThenInclude(dp => dp.Producto)
                .Where(p => p.FechaPago.Date >= inicio.Date && p.FechaPago.Date <= fin.Date)
                .ToListAsync();

            var productosVendidos = pagos
                .Where(p => p.Pedido != null)
                .SelectMany(p => p.Pedido!.DetallesPedido)
                .Where(dp => dp.Producto != null)
                .GroupBy(dp => dp.Producto!.Nombre)
                .Select(g => new ReporteProducto
                {
                    Producto = g.Key,
                    Total = g.Sum(d => d.PrecioUnitario * d.Cantidad)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            var ingresosPorFecha = pagos
                .GroupBy(p => p.FechaPago.Date)
                .Select(g => new ReporteIngreso
                {
                    Fecha = g.Key.ToShortDateString(),
                    Total = g.Sum(p => p.Monto)
                })
                .OrderBy(x => x.Fecha)
                .ToList();

            var stockDisponible = await _context.Productos
                .Select(p => new ReporteStock
                {
                    Nombre = p.Nombre,
                    Stock = p.Stock
                })
                .OrderByDescending(p => p.Stock)
                .Take(10)
                .ToListAsync();

            Console.WriteLine($"\n📌 Total de pagos encontrados entre {inicio:dd/MM/yyyy} y {fin:dd/MM/yyyy}: {pagos.Count}");

            foreach (var pago in pagos)
            {
                Console.WriteLine($"\n🧾 Pago ID: {pago.IdPago}");
                Console.WriteLine($"   Fecha: {pago.FechaPago:dd/MM/yyyy HH:mm}");
                Console.WriteLine($"   Monto: {pago.Monto:C}");

                if (pago.Pedido != null)
                {
                    Console.WriteLine($"   Pedido ID: {pago.Pedido.IdPedido}, Estado: {pago.Pedido.Estado}");

                    if (pago.Pedido.DetallesPedido.Any())
                    {
                        foreach (var detalle in pago.Pedido.DetallesPedido)
                        {
                            Console.WriteLine($"      🔸 Producto: {detalle.Producto?.Nombre ?? "N/D"}");
                            Console.WriteLine($"         Cantidad: {detalle.Cantidad}");
                            Console.WriteLine($"         Precio: {detalle.PrecioUnitario:C}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("      ⚠️ El pedido no tiene detalles.");
                    }
                }
                else
                {
                    Console.WriteLine("   ⚠️ No hay pedido asociado a este pago.");
                }
            }

            Console.WriteLine("\n📊 Productos más vendidos:");
            if (productosVendidos.Any())
            {
                foreach (var producto in productosVendidos)
                {
                    Console.WriteLine($"   - {producto.Producto}: {producto.Total:C}");
                }
            }
            else
            {
                Console.WriteLine("   ❌ No hay productos vendidos en el rango de fechas.");
            }

            Console.WriteLine("\n💰 Ingresos por fecha:");
            if (ingresosPorFecha.Any())
            {
                foreach (var ingreso in ingresosPorFecha)
                {
                    Console.WriteLine($"   - {ingreso.Fecha}: {ingreso.Total:C}");
                }
            }
            else
            {
                Console.WriteLine("   ❌ No hay ingresos registrados en el rango de fechas.");
            }

            Console.WriteLine("\n📦 Top 10 productos con más stock disponible:");
            if (stockDisponible.Any())
            {
                foreach (var prod in stockDisponible)
                {
                    Console.WriteLine($"   - {prod.Nombre}: {prod.Stock} unidades");
                }
            }
            else
            {
                Console.WriteLine("   ❌ No se encontró información de stock.");
            }


            if (formato == "pdf")
            {
                var pdfBytes = GenerarPdfReporte(productosVendidos, ingresosPorFecha, stockDisponible, inicio, fin);
                return File(pdfBytes, "application/pdf", $"Reporte_{tipo}_{fecha:yyyyMMdd}.pdf");
            }
            else
            {
                var excelBytes = GenerarExcelReporte(productosVendidos, ingresosPorFecha, stockDisponible, inicio, fin);
                return File(excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Reporte_{tipo}_{fecha:yyyyMMdd}.xlsx");
            }
        }

        private byte[] GenerarPdfReporte(List<ReporteProducto> productosVendidos, List<ReporteIngreso> ingresosPorFecha, List<ReporteStock> stockDisponible, DateTime inicio, DateTime fin)
        {
            var chartProductos = GenerarGraficoDeBarras(productosVendidos.Select(x => x.Producto).ToList(), productosVendidos.Select(x => (float)x.Total).ToList(), "Productos más Vendidos");
            var chartIngresos = GenerarGraficoDeBarras(ingresosPorFecha.Select(x => x.Fecha).ToList(), ingresosPorFecha.Select(x => (float)x.Total).ToList(), "Ingresos por Fecha");
            var chartStock = GenerarGraficoDeBarras(stockDisponible.Select(x => x.Nombre).ToList(), stockDisponible.Select(x => (float)x.Stock).ToList(), "Stock Disponible");

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.Header().Text($"Reporte de Ventas ({inicio:dd/MM/yyyy} - {fin:dd/MM/yyyy})").FontSize(18).Bold().AlignCenter();
                    page.Content().Column(col =>
                    {
                        col.Spacing(20);
                        col.Item().Image(chartProductos);
                        col.Item().Image(chartIngresos);
                        col.Item().Image(chartStock);
                    });
                    page.Footer().AlignCenter().Text("Generado automáticamente - © TuEmpresa");
                });
            });

            return pdf.GeneratePdf();
        }

        private byte[] GenerarExcelReporte(List<ReporteProducto> productosVendidos, List<ReporteIngreso> ingresosPorFecha, List<ReporteStock> stockDisponible, DateTime inicio, DateTime fin)
        {
            using var wb = new XLWorkbook();
            var ws1 = wb.Worksheets.Add("Productos Más Vendidos");
            var ws2 = wb.Worksheets.Add("Ingresos por Fecha");
            var ws3 = wb.Worksheets.Add("Stock Disponible");

            ws1.Cell(1, 1).Value = "Producto";
            ws1.Cell(1, 2).Value = "Total";
            for (int i = 0; i < productosVendidos.Count; i++)
            {
                ws1.Cell(i + 2, 1).Value = productosVendidos[i].Producto;
                ws1.Cell(i + 2, 2).Value = productosVendidos[i].Total;
            }
            ws1.RangeUsed().CreateTable();

            ws2.Cell(1, 1).Value = "Fecha";
            ws2.Cell(1, 2).Value = "Total";
            for (int i = 0; i < ingresosPorFecha.Count; i++)
            {
                ws2.Cell(i + 2, 1).Value = ingresosPorFecha[i].Fecha;
                ws2.Cell(i + 2, 2).Value = ingresosPorFecha[i].Total;
            }
            ws2.RangeUsed().CreateTable();

            ws3.Cell(1, 1).Value = "Producto";
            ws3.Cell(1, 2).Value = "Stock";
            for (int i = 0; i < stockDisponible.Count; i++)
            {
                ws3.Cell(i + 2, 1).Value = stockDisponible[i].Nombre;
                ws3.Cell(i + 2, 2).Value = stockDisponible[i].Stock;
            }
            ws3.RangeUsed().CreateTable();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }

        private byte[] GenerarGraficoDeBarras(List<string> etiquetas, List<float> valores, string titulo)
        {
            int width = 800, height = 400;
            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            var barPaint = new SKPaint { Color = SKColors.SteelBlue, Style = SKPaintStyle.Fill };
            var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 16 };
            var titlePaint = new SKPaint { Color = SKColors.Black, TextSize = 20, IsAntialias = true };

            float barWidth = width / (valores.Count * 2f);
            float maxValor = valores.Count > 0 ? valores.Max() : 1;
            float scale = (height - 100) / maxValor;

            canvas.DrawText(titulo, 20, 30, titlePaint);

            for (int i = 0; i < valores.Count; i++)
            {
                float x = i * 2 * barWidth + 50;
                float y = height - (valores[i] * scale) - 40;
                canvas.DrawRect(x, y, barWidth, valores[i] * scale, barPaint);
                canvas.DrawText(valores[i].ToString("0.##"), x, y - 5, textPaint);
                canvas.DrawText(etiquetas[i], x, height - 20, textPaint);
            }

            using var img = SKImage.FromBitmap(bitmap);
            using var data = img.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
    }

    public class ReporteProducto
    {
        public string Producto { get; set; } = "";
        public decimal Total { get; set; }
    }

    public class ReporteIngreso
    {
        public string Fecha { get; set; } = "";
        public decimal Total { get; set; }
    }

    public class ReporteStock
    {
        public string Nombre { get; set; } = "";
        public int Stock { get; set; }
    }
}
