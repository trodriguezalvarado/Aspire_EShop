using Microsoft.EntityFrameworkCore;
using TiendaAspire.ApiService.Data.Models;
using TiendaAspire.Data.Clases;

namespace TiendaAspire.ApiService.Data
{
    public class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
    {
        public DbSet<ProductoCatalogo> Productos => Set<ProductoCatalogo>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Define que el precio tendrá un máximo de 18 dígitos, 2 de ellos decimales
            modelBuilder.Entity<ProductoCatalogo>()
                .Property(p => p.Precio)
                .HasColumnType("decimal(18,2)");
        }
    }

    
}
