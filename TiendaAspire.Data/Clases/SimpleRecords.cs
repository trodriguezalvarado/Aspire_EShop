namespace TiendaAspire.Data.Clases
{
    public record ProductoInfo(Guid Id, string Nombre, int Existencias, string Status);
    public record ProductoCatalogoResponse(Guid Id, string Nombre, int Existencias, decimal? precio, string Status);
    public record StockUpdatedEvent(Guid CodigoUnico, string Nombre, int Cantidad);


    public record StockInput(string Nombre, int Cantidad);

    public record ActivarProductoRequest(decimal Precio);

    public class Producto
    {
        public int Id { get; set; } // Internal SQL ID
        public Guid CodigoUnico { get; set; } // The Correlation ID (GUID)
        public string Nombre { get; set; } = string.Empty;
        public int Stock { get; set; }
    }
}
