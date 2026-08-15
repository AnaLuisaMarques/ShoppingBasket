using Microsoft.Extensions.Caching.Memory;
using ShoppingBasket.API.Interfaces;
using ShoppingBasket.Contracts.DTOs;
using System.Net.Http.Headers;

namespace ShoppingBasket.API.Services;

public class ProductCatalogClient : IProductCatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProductCatalogClient> _logger;
    private readonly ITokenProvider _tokenProvider;
    private const string CacheKeyAllProducts = "CodeChallenge_AllProducts";

    public ProductCatalogClient(HttpClient httpClient, IMemoryCache cache, ILogger<ProductCatalogClient> logger, ITokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _tokenProvider = tokenProvider;
    }

    public async Task<ProductDto?> GetProductAsync(string productId)
    {
        var all = await GetAllProductsCachedAsync();
        return all.FirstOrDefault(p => string.Equals(p.Id.ToString(), productId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IList<ProductDto>> GetTop100ProductsAsync()
    {
        var all = await GetAllProductsCachedAsync();
        return all.Take(100).ToList();
    }

    public async Task<(IList<ProductDto> Items, int TotalCount)> GetProductsAsync(int pageNumber, int pageSize)
    {
        var all = await GetAllProductsCachedAsync();

        // Order by price ascending
        var ordered = all.OrderBy(p => p.Price).ToList();

        var total = ordered.Count;
        
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;

        var skip = (pageNumber - 1) * pageSize;
        
        IList<ProductDto> items;
        if (skip >= total)
        {
            items = new List<ProductDto>();
        }
        else
        {
            items = ordered.Skip(skip).Take(pageSize).ToList();
        }

        return (items, total);
    }

    private async Task<List<ProductDto>> GetAllProductsCachedAsync()
    {
        if (_cache.TryGetValue(CacheKeyAllProducts, out List<ProductDto>? cached) && cached != null)
        {
            return cached;
        }

        await _tokenProvider.EnsureTokenAsync();
        var token = _tokenProvider.CurrentToken;
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        List<ProductDto>? products = null;
        try
        {
            products = await _httpClient.GetFromJsonAsync<List<ProductDto>>("GetAllProducts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch products from Code Challenge API");
            products = new List<ProductDto>();
        }

        products ??= new List<ProductDto>();
        _cache.Set(CacheKeyAllProducts, products, TimeSpan.FromHours(24));
        return products;
    }
}
