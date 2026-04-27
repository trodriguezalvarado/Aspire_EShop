using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaAspire.ApiService.Data.Models
{
    public class ProductoCatalogo
    {
        public int Id { get; set; } // Internal SQL ID
        public Guid CodigoUnico { get; set; } // The Correlation ID (GUID)
        public string Nombre { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal? Precio { get; set; }
    }
}
