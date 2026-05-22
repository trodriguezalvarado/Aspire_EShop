using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using System.Text.Json;
using TiendaAspire.ApiService.Data;
using TiendaAspire.ApiService.Data.Models;
using TiendaAspire.Data.Clases;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TiendaAspire.ApiService.Endpoints
{
    public static class CatalogoEndpoints
    {
        public static void MapCatalogoEndpoints(this IEndpointRouteBuilder routes)
        {
            routes.MapPost("/internal/dapr-stock-receiver", async (Producto stockData, CatalogoDbContext db, DaprClient daprClient) =>
            {
                // This log confirms the message completed the entire decoupled loop!
                Console.WriteLine($"[CATALOGO SUCCESS] Stock update event caught for: {stockData.Nombre} (Stock: {stockData.Stock})");

                // Your database update logic goes here
                var product = await db.Productos.FirstOrDefaultAsync(p => p.CodigoUnico == stockData.CodigoUnico);
                if (product != null)
                {
                    product.Stock = stockData.Stock; ; // Sync the stock count
                    
                }
                else
                {
                    product = new Data.Models.ProductoCatalogo
                    {
                        CodigoUnico = stockData.CodigoUnico,
                        Nombre = $"{stockData.Nombre}: Producto Sincronizado", // In a real app, 'Nombre' would be in the message too
                        Stock = stockData.Stock
                    };
                    db.Productos.Add(product);
                }
                await db.SaveChangesAsync();
                await daprClient.DeleteStateAsync("statestore", product.CacheKey);
                // Return a 200 OK so Dapr knows the message was successfully processed 
                // and can clear it from the RabbitMQ queue
                return Results.Ok();
            })
    .WithTopic("rabbit-pubsub", "stockUpdated");

            var group = routes.MapGroup("/catalogo");
            group.MapGet("/", async (CatalogoDbContext db) =>
            {
                var productos = await db.Productos.Where(p => p.Precio != null && p.Stock > 0).ToListAsync();
                var productosResponse = new List<ProductoCatalogoResponse>();
                for (int i = 0; i < productos.Count; i++)
                {
                    var producto = productos[i];
                    productosResponse.Add(new ProductoCatalogoResponse(producto.CodigoUnico, producto.Nombre, producto.Stock, producto.Precio, ""));
                }
                return Results.Ok(productosResponse);

            });

            group.MapGet("/pendientes", async (CatalogoDbContext db) =>
            {
                var productosPendientes = await db.Productos.Where(p => p.Precio == null && p.Stock > 0).ToListAsync();
                var productosResponse = new List<ProductoCatalogoResponse>();
                for (int i = 0; i < productosPendientes.Count; i++)
                {
                    var producto = productosPendientes[i];
                    productosResponse.Add(new ProductoCatalogoResponse(producto.CodigoUnico, producto.Nombre, producto.Stock, producto.Precio, ""));
                }
                return Results.Ok(productosResponse);
            }).RequireAuthorization(policy => policy.RequireRole("Catalog_Manager"));

            group.MapGet("/{codigoUnico:guid}", async (Guid codigoUnico, CatalogoDbContext db, DaprClient daprClient) =>
            {
                // 1. Try Redis first
                var cachedproducto = await daprClient.GetStateAsync<ProductoCatalogo>(
                    storeName: "statestore",
                    key: ProductoCatalogo.CacheKeyBuilder(codigoUnico)
                );
                if (cachedproducto != null)
                {
                    var info = new ProductoCatalogoResponse(cachedproducto.CodigoUnico, cachedproducto.Nombre, cachedproducto.Stock, cachedproducto.Precio,"Producto obtenido desde cache");                   
                    return Results.Ok(info);
                }

                // 2. Try Catalog SQL (The local copy)
                var producto = await db.Productos.FirstOrDefaultAsync(p => p.CodigoUnico == codigoUnico && p.Precio != null && p.Stock > 0);

                if (producto != null)
                {
                    // Configure the 1-minute expiration metadata (TTL)
                    var cacheMetadata = new Dictionary<string, string>
                    {
                        { "ttlInSeconds", "60" } // Automatically deletes itself from Redis after 60 seconds
                    };
                    // Save directly to the store
                    await daprClient.SaveStateAsync(
                        storeName: "statestore",
                        key: producto.CacheKey,
                        value: producto, // Direct C# object
                        metadata: cacheMetadata
                    );
                    var info = new ProductoCatalogoResponse(producto.CodigoUnico, producto.Nombre, producto.Stock, producto.Precio,"Producto obtenido desde bd local, actualizada desde Inventario");
                    return Results.Ok(info);
                }

                // 3. If not found, return 404 (In Event-Driven, we don't call Inventory via HTTP)
                return Results.NotFound(new { Message = "Producto no sincronizado aún." });
            });

            group.MapPut("/{id:guid}/activar", async (Guid id, ActivarProductoRequest req, CatalogoDbContext db, DaprClient daprClient) =>
            {
                var producto = await db.Productos.FirstOrDefaultAsync(p => p.CodigoUnico == id);
                if (producto is null) return Results.NotFound();

                producto.Precio = req.Precio; // Al asignar precio, IsAvailable será true

                await db.SaveChangesAsync();

                // IMPORTANTE: Limpiar la caché de Redis para que el cambio se vea en la tienda Blazor
                await daprClient.DeleteStateAsync("statestore", producto.CacheKey);

                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Catalog_Manager"));
        }
    }
}
