using System.Net.Http.Json;
using TiendaAspire.Data.Clases;

namespace TiendaAspire.Web
{
    public class CatalogClient(HttpClient httpClient)
    {
        public async Task<List<ProductoInfo>> GetProductos()
        {
            return await httpClient.GetFromJsonAsync<List<ProductoInfo>>("/catalogo");
        }
        public async Task<ProductoInfo?> GetProductoDetalleAsync(Guid id)
        {
            // Calling our catalog endpoint that joins with inventory
            return await httpClient.GetFromJsonAsync<ProductoInfo>($"/catalogo/{id}");
        }
    }

    
}
