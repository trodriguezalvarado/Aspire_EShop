using TiendaAspire.Web;
using TiendaAspire.Web.Components;
using Dapr.Client;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder.Services.AddDaprClient();

builder.Services.AddScoped(sp =>
    DaprClient.CreateInvokeHttpClient(appId: "catalogoservice"));

builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://apiservice");
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    });
//builder.Services.AddHttpClient<CatalogClient>(client =>
//{
//    // Use the name defined in the AppHost
//    client.BaseAddress = new("http://catalogoservice");
//})
//    .AddServiceDiscovery()
//    .AddStandardResilienceHandler();

builder.Services.AddHttpClient<CatalogClient>(client =>
{
    // 1. Point the base address directly to your local Dapr sidecar endpoint port
    // (Aspire sets the DAPR_HTTP_PORT variable automatically)
    var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";
    client.BaseAddress = new Uri($"http://localhost:{daprHttpPort}/");

    // 2. THE FIX: Force the explicit Dapr destination App ID into the request headers
    // This tells the Dapr sidecar EXACTLY which microservice must receive the request
    client.DefaultRequestHeaders.Add("dapr-app-id", "catalogoservice");
})
.AddStandardResilienceHandler();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseOutputCache();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
