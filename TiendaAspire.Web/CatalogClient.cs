using System.Net.Http.Json;
using TiendaAspire.Data.Clases;

namespace TiendaAspire.Web
{
    public class CatalogClient(HttpClient httpClient)
    {
        public async Task<List<ProductoCatalogoResponse>> GetProductos()
        {
            return await httpClient.GetFromJsonAsync<List<ProductoCatalogoResponse>>("/catalogo");
        }
        public async Task<ProductoCatalogoResponse?> GetProductoDetalleAsync(Guid id)
        {
            // Calling our catalog endpoint that joins with inventory
            return await httpClient.GetFromJsonAsync<ProductoCatalogoResponse>($"/catalogo/{id}");
        }
    }

    
}
