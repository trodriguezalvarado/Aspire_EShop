using Aspire.Hosting;
using Aspire.Hosting.Dapr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.Configure<DaprOptions>(options =>
{
    options.DaprPath = @"D:\dapr\dapr.exe";
});

var cache = builder.AddRedis("cache", 14470);

//var stateStore = builder.AddDaprStateStore("statestore");


var sql = builder.AddSqlServer("sqlserver")
    /*.WithDataVolume()*/;

// 1. Agregar RabbitMQ
var messaging = builder.AddRabbitMQ("messaging",null, null, 5672)
    .WithManagementPlugin()
    .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
    .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
    .WithEnvironment("RABBITMQ_SERVER_ADDITIONAL_ERL_ARGS", "-rabbit reverse_dns_lookup false") // Disables slow DNS lookups
    .WithEnvironment("RABBITMQ_LOGS", "-"); // Forces logs directly to console, saving I/O disk writes;

var pubSub = builder.AddDaprPubSub("pubsub");

// 2. Base de datos para Catálogo
var catalogDb = sql.AddDatabase("catalogdb");

var keycloak = builder.AddKeycloak("keycloak", port: 8081)
                      .WithDataVolume(); // Mantiene tus usuarios y roles si reinicias el contenedor



// 3. Base de datos para Inventario
var inventoryDb = sql.AddDatabase("inventorydb");

var rabbitEndpoint = messaging.GetEndpoint("tcp");

var inventario = builder.
    AddProject<Projects.InventarioService>("inventarioservice")
    .WithDaprSidecar(sidecarBuilder => {
        // This specifically injects the variable into the sidecar process
        sidecarBuilder.Resource.Annotations.Add(new CommandLineArgsCallbackAnnotation(args => {
            args.Add("--initial-wait-timeout");
            args.Add("45000");
        }));
        
       sidecarBuilder.Resource.Annotations.Add(new EnvironmentCallbackAnnotation(async envContext => {
           // Force guest:guest matching the environment variables we set on RabbitMQ
           var host = rabbitEndpoint.Host;
           var port = rabbitEndpoint.Port;

           envContext.EnvironmentVariables["RABBITMQ_URI"] = $"amqp://guest:guest@{host}:{port}";
           await Task.Delay(20000);
       }));
       
    } )
    .WithReference(inventoryDb)
    .WithReference(keycloak)
    /*.WithReference(stateStore)
    //.WithReference(pubSub)*/;
//inventario.WithEnvironment("RABBITMQ_URI", () =>
//    $"amqp://guest:guest@{rabbitEndpoint.Property(EndpointProperty.Host)}:{rabbitEndpoint.Property(EndpointProperty.Port)}");

var apiService = builder.AddProject<Projects.TiendaAspire_ApiService>("catalogoservice")
    .WithDaprSidecar(sidecarBuilder => {

        sidecarBuilder.Resource.Annotations.Add(new EnvironmentCallbackAnnotation(async envContext => {
            await Task.Delay(20000);
        }));

    })          // <--- Esencial
    .WithReference(catalogDb)   // Directo a SQL
    .WithReference(keycloak)    // Directo a Auth
    /*.WithReference(pubSub)      // Dapr para escuchar eventos
    .WithReference(stateStore)*/;

builder.AddProject<Projects.TiendaAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithDaprSidecar()
    .WithReference(apiService);

var adminDash = builder.AddNpmApp("admin-ui", "../TiendaAspire.admin-dashBoard")
    .WithHttpEndpoint(port: 5100, isProxied: false)
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WithReference(inventario)    
    .WithReference(keycloak); 

builder.AddProject<Projects.TiendaAspire_Data>("tiendaaspire-data");
builder.Build().Run();
