using ShoppingBasket.API.Interfaces;
using ShoppingBasket.API.Services;
using ShoppingBasket.API.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IShoppingBasketService, ShoppingBasketService>();

builder.Services.AddSingleton<IShoppingBasketRepository, ShoppingBasketRepository>();
builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<IProductCatalogClient, ProductCatalogClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["CodeChallengeApi:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl)) client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<ITokenProvider, TokenProvider>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["CodeChallengeApi:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl)) client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
