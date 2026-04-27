var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var sql = builder.AddSqlServer("sqlserver");

// 1. Agregar RabbitMQ
var messaging = builder.AddRabbitMQ("messaging");

// 2. Base de datos para Catálogo
var catalogDb = sql.AddDatabase("catalogdb");

var keycloak = builder.AddKeycloak("keycloak", port: 8081)
                      .WithDataVolume(); // Mantiene tus usuarios y roles si reinicias el contenedor



// 3. Base de datos para Inventario
var inventoryDb = sql.AddDatabase("inventorydb");

var inventario = builder.
    AddProject<Projects.InventarioService>("inventarioservice")
    .WithReference(inventoryDb)
    .WithReference(messaging)
    .WithReference(keycloak);

var apiService = builder.AddProject<Projects.TiendaAspire_ApiService>("catalogoservice")
    .WithReference(catalogDb)
    .WithReference(messaging)
    .WithReference(inventario)
    .WithReference(cache)
    .WithReference(keycloak);

builder.AddProject<Projects.TiendaAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

var adminDash = builder.AddNpmApp("admin-ui", "../TiendaAspire.admin-dashBoard")
    .WithHttpEndpoint(port: 5100, isProxied: false)
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WithReference(inventario)    
    .WithReference(keycloak); 

builder.AddProject<Projects.TiendaAspire_Data>("tiendaaspire-data");
builder.Build().Run();
