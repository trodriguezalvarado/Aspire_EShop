using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TiendaAspire.ApiService.Data;
using TiendaAspire.ApiService.Endpoints;
using TiendaAspire.ApiService.Worker;
using Polly;
using Polly.Retry;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();

// En el ApiService
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new() { Title = "Catálogo API", Version = "v1" });
    // 1. Definir el esquema de seguridad
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Introduce el token JWT de Keycloak."
    });
    // 2. Aplicar el candado a los endpoints protegidos
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpClient("InventarioClient", static client =>
{
    // El nombre "inventarioservice" es el que pusimos en el AppHost
    client.BaseAddress = new("http://inventarioservice");
})
.AddServiceDiscovery();

builder.AddRedisDistributedCache("cache");

builder.AddSqlServerDbContext<CatalogoDbContext>("catalogdb");
builder.AddRabbitMQClient("messaging", settings =>
{
    settings.DisableHealthChecks = true;
    settings.ConnectionString = settings.ConnectionString?.Replace("amqps://", "amqp://");
}); // The Aspire component
builder.Services.AddHostedService<StockUpdateWorker>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin() // In production, replace with your Angular URL
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer("keycloak", realm: "TiendaRealm", options =>
    {
        options.RequireHttpsMetadata = false;
        options.Audience = "account";
        var authority = builder.Configuration["Authentication:Schemes:Bearer:Authority"]
                        ?? "http://localhost:8081/realms/TiendaRealm";
        // Force the authority so the service knows where to download the keys
        options.Authority = authority;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidIssuer = authority,
            ValidateAudience = false,
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            // DO NOT use SignatureValidator here unless you are doing manual low-level crypto
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseCors(); // This must be placed BEFORE MapControllers or MapEndpoints

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapCatalogoEndpoints();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
    // Usando ResiliencePipelineBuilder
    var pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            Delay = TimeSpan.FromSeconds(2),
            MaxRetryAttempts = 5,
            BackoffType = DelayBackoffType.Exponential,
            OnRetry = args =>
            {
                logger.LogWarning("Fallo en BD. Intento {0}. Reintentando...", args.AttemptNumber);
                return default;
            }
        })
        .Build();

    await pipeline.ExecuteAsync(async token =>
    {
        await context.Database.MigrateAsync(token);
    }, CancellationToken.None);
    //await context.Database.EnsureCreatedAsync();
    //await context.Database.MigrateAsync();
}

app.Run();

// Clase auxiliar para recibir el JSON
public record StockResponse(int ProductoId, int Cantidad);
