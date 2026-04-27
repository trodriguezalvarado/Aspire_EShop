using System.Text.Json;
using System.Text;
using RabbitMQ.Client;
using TiendaAspire.Data.Clases;
using InventarioService.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using System.Security.Claims;

namespace InventarioService.Endpoints
{
    public static class InventarioEndpoints
    {
        public static void MapInventarioEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/stock");

            group.MapGet("/diagnostico-claims", (ClaimsPrincipal user) =>
            {
                return user.Claims.Select(c => new { c.Type, c.Value });
            }).RequireAuthorization();

            //Listado general
            group.MapGet("/", async (InventarioDbContext db) =>
            await db.Stocks.ToListAsync()).RequireAuthorization(policy => policy.RequireRole("Product_Manager"));

            group.MapGet("/{codigoUnico:guid}", async (Guid codigoUnico, InventarioDbContext db) =>
            {
                // Search by the Correlation ID (GUID)
                var stock = await db.Stocks.FirstOrDefaultAsync(s => s.CodigoUnico == codigoUnico);

                return stock is not null
                    ? Results.Ok(stock)
                    : Results.NotFound(new { Message = $"Stock con código {codigoUnico} no encontrado." });
            }).RequireAuthorization(policy => policy.RequireRole("Product_Manager"));

            group.MapPost("/", async (StockInput input, InventarioDbContext db, IConnection rabbitConnection) =>
            {
                var nuevoStock = new Producto
                {
                    CodigoUnico = Guid.NewGuid(),
                    Nombre = input.Nombre,
                    Stock = input.Cantidad
                };

                db.Stocks.Add(nuevoStock);
                await db.SaveChangesAsync();
                try
                {
                    using var channel = rabbitConnection.CreateModel();
                    channel.QueueDeclare(queue: "stock_updates",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false);
                    var messagePayload = new StockUpdatedEvent(nuevoStock.CodigoUnico, nuevoStock.Nombre, nuevoStock.Stock);
                    var messageJson = JsonSerializer.Serialize(messagePayload);
                    var body = Encoding.UTF8.GetBytes(messageJson);
                    channel.BasicPublish(exchange: string.Empty,
                                 routingKey: "stock_updates",
                                 body: body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error enviando mensaje: {ex.Message}");
                    return Results.Accepted($"/stock/{nuevoStock.CodigoUnico}", new { nuevoStock, Warning = "Stock guardado pero notificación de red pendiente." });
                }
                

                // The URL now matches the GUID pattern
                return Results.Created($"/stock/{nuevoStock.CodigoUnico}", nuevoStock);
            }).RequireAuthorization(policy => policy.RequireRole("Product_Manager"));

            //Actualización
            group.MapPut("/{id:guid}", async (Guid id, StockInput dto, InventarioDbContext db, IConnection rabbitConnection) =>
            {
                var producto = await db.Stocks.FirstAsync(p => p.CodigoUnico == id);
                if (producto is null) return Results.NotFound();

                producto.Stock = dto.Cantidad;
                await db.SaveChangesAsync();

                // Notificar al catálogo que el stock cambió
                using var channel = rabbitConnection.CreateModel();
                channel.QueueDeclare(queue: "stock_updates",
                             durable: true,
                             exclusive: false,
                             autoDelete: false);
                var messagePayload = new StockUpdatedEvent(producto.CodigoUnico, producto.Nombre, producto.Stock);
                var messageJson = JsonSerializer.Serialize(messagePayload);
                var body = Encoding.UTF8.GetBytes(messageJson);
                channel.BasicPublish(exchange: string.Empty,
                             routingKey: "stock_updates",
                             body: body);

                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Product_Manager"));
        }
    }
}
