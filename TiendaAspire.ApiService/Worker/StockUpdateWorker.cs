using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.EntityFrameworkCore;
using TiendaAspire.Data.Clases;
using TiendaAspire.ApiService.Data;
using Microsoft.Extensions.Caching.Distributed;

namespace TiendaAspire.ApiService.Worker
{
    
    public class StockUpdateWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _rabbitConnection;
        private readonly ILogger<StockUpdateWorker> _logger;
        private readonly IDistributedCache _cache;
        private IModel? _channel;

        public StockUpdateWorker(IServiceProvider serviceProvider, IConnection rabbitConnection, ILogger<StockUpdateWorker> logger, IDistributedCache cache)
        {
            _serviceProvider = serviceProvider;
            _rabbitConnection = rabbitConnection;
            _logger = logger;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(15000, stoppingToken);
            try
            {
                _logger.LogInformation("Intentando conectar a RabbitMQ...");
                _channel = _rabbitConnection.CreateModel();
                _channel.QueueDeclare(queue: "stock_updates", durable: true, exclusive: false, autoDelete: false);

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    var deliveryTag = ea.DeliveryTag;
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    // Ensure this record matches your Inventory's JSON: { CodigoUnico, Cantidad }
                    var stockEvent = JsonSerializer.Deserialize<StockUpdatedEvent>(message);

                    if (stockEvent != null)
                    {
                        _logger.LogInformation("Evento recibido: Nombre del producto -> {Nombre}, GUID {Guid} -> Cant {Cant}", stockEvent.Nombre, stockEvent.CodigoUnico, stockEvent.Cantidad);
                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();

                            // 1. Search by GUID, not by integer ID
                            var producto = await db.Productos.FirstOrDefaultAsync(p => p.CodigoUnico == stockEvent.CodigoUnico, stoppingToken);

                            if (producto != null)
                            {
                                // 2. Update existing
                                producto.Stock = stockEvent.Cantidad;
                                producto.Nombre = $"{stockEvent.Nombre}: : Producto Sincronizado";
                                _logger.LogInformation("Stock actualizado para producto existente, Producto actualizado -> {Nombre}, Stock actual -> {Cantidad}.", stockEvent.Nombre, stockEvent.Cantidad);
                                var cacheKey = $"product-{stockEvent.CodigoUnico}";
                                await _cache.RemoveAsync(cacheKey);
                            }
                            else
                            {
                                // 3. Create new (Upsert)
                                _logger.LogInformation("Producto no encontrado en Catálogo. Creando nuevo registro...");
                                db.Productos.Add(new Data.Models.ProductoCatalogo
                                {
                                    CodigoUnico = stockEvent.CodigoUnico,
                                    Nombre = $"{stockEvent.Nombre}: Producto Sincronizado", // In a real app, 'Nombre' would be in the message too
                                    Stock = stockEvent.Cantidad
                                });
                            }
                            _channel.BasicAck(deliveryTag: deliveryTag, multiple: false);
                            await db.SaveChangesAsync(stoppingToken);
                        }
                        catch (Exception)
                        {
                            _channel.BasicNack(deliveryTag: deliveryTag, multiple: false, requeue: true);
                        }
                    };
                };
                _logger.LogInformation("Conectado exitosamente a RabbitMQ.");
                _channel.BasicConsume(queue: "stock_updates", autoAck: false, consumer: consumer);
            }
            catch (Exception)
            {
                _logger.LogWarning("RabbitMQ aún no está listo. ");                
                await Task.Delay(5000, stoppingToken);
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

        }

        // Ensure this matches exactly what the Inventory sends

    }

}
