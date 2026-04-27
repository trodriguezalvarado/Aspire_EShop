using InventarioService.Data;
using InventarioService.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly.Retry;
using Polly;
using TiendaAspire.Data.Clases;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new() { Title = "Inventario API", Version = "v1" });
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

builder.AddSqlServerDbContext<InventarioDbContext>("inventorydb");
builder.AddRabbitMQClient("messaging");

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
            ValidIssuer = authority, // Usamos la misma variable corregida
            ValidateAudience = false,
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            ValidateLifetime = true
        };
    });


//builder.Services.AddAuthentication()
//                .AddKeycloakJwtBearer("keycloak", realm: "TiendaRealm", options =>
//                {
//                    options.RequireHttpsMetadata = false;
//                    options.Audience = "account";
//                    options.TokenValidationParameters = new TokenValidationParameters
//                    {
//                        ValidateIssuer = true,
//                        // If it still fails, set this to false only for debugging:
//                        // ValidateIssuer = false, 
//                        ValidIssuer = "http://localhost:8081/realms/TiendaRealm",

//                        ValidateAudience = false, // Sometimes the audience is 'account' or the Client ID
//                        RoleClaimType = "role",
//                        //SignatureValidator = delegate (string token, TokenValidationParameters parameters)
//                        //{
//                        //    var jwt = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token);
//                        //    return jwt;
//                        //}
//                    };
//options.Authority = "http://localhost:8081/realms/TiendaRealm";
//                    options.SaveToken = true;
//                });
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

app.MapDefaultEndpoints();
app.MapInventarioEndpoints();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<InventarioDbContext>();
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
    //await context.Database.MigrateAsync();
}

app.Run();
