using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using TiendaAspire.ApiService.Data;
using TiendaAspire.Data.Clases;

namespace TiendaAspire.ApiService.Endpoints
{
    public static class CatalogoEndpoints
    {
        public static void MapCatalogoEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/catalogo");
            group.MapGet("/", async (CatalogoDbContext db) =>
            {
                var productos = await db.Productos.Where(p => p.Precio != null && p.Stock > 0).ToListAsync();
                var productosResponse = new List<ProductoCatalogoResponse>();
                for (int i = 0; i < productos.Count; i++)
                {
                    var producto = productos[i];
                    productosResponse.Add(new ProductoCatalogoResponse(producto.CodigoUnico, producto.Nombre, producto.Stock, producto.Precio));
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
                    productosResponse.Add(new ProductoCatalogoResponse(producto.CodigoUnico, producto.Nombre, producto.Stock, producto.Precio));
                }
                return Results.Ok(productosResponse);
            }).RequireAuthorization(policy => policy.RequireRole("Catalog_Manager"));

            group.MapGet("/{codigoUnico:guid}", async (Guid codigoUnico, CatalogoDbContext db, IDistributedCache cache) =>
            {
                string cacheKey = $"product-{codigoUnico}";

                // 1. Try Redis first
                var cached = await cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cached))
                {
                    var cachedproducto = JsonSerializer.Deserialize<Producto>(cached);
                    var info = new ProductoInfo(cachedproducto.CodigoUnico, cachedproducto.Nombre, cachedproducto.Stock, "Producto obtenido desde cache");                   
                    return Results.Ok(info);
                }

                // 2. Try Catalog SQL (The local copy)
                var producto = await db.Productos.FirstOrDefaultAsync(p => p.CodigoUnico == codigoUnico && p.Precio != null && p.Stock > 0);

                if (producto != null)
                {
                    // Sync with Redis
                    await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(producto));
                    var info = new ProductoInfo(producto.CodigoUnico, producto.Nombre, producto.Stock, "Producto obtenido desde bd local, actualizada desde Inventario");
                    return Results.Ok(info);
                }

                // 3. If not found, return 404 (In Event-Driven, we don't call Inventory via HTTP)
                return Results.NotFound(new { Message = "Producto no sincronizado aún." });
            });

            group.MapPut("/{id:guid}/activar", async (Guid id, ActivarProductoRequest req, CatalogoDbContext db, IDistributedCache cache) =>
            {
                var producto = await db.Productos.FirstOrDefaultAsync(p => p.CodigoUnico == id);
                if (producto is null) return Results.NotFound();

                producto.Precio = req.Precio; // Al asignar precio, IsAvailable será true

                await db.SaveChangesAsync();

                // IMPORTANTE: Limpiar la caché de Redis para que el cambio se vea en la tienda Blazor
                await cache.RemoveAsync($"product-{id}");

                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Catalog_Manager"));
        }
    }
}
