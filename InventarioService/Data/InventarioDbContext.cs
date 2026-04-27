using Microsoft.EntityFrameworkCore;
using TiendaAspire.Data.Clases;

namespace InventarioService.Data
{
    public class InventarioDbContext(DbContextOptions<InventarioDbContext> options) : DbContext(options)
    {
        public DbSet<Producto> Stocks => Set<Producto>();
    }
}
