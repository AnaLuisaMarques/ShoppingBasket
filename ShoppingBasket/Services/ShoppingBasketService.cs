using ShoppingBasket.API.Interfaces;
using ShoppingBasket.Contracts.DTOs;
using Microsoft.Extensions.Logging;

namespace ShoppingBasket.API.Services;

public class ShoppingBasketService : IShoppingBasketService
{
    private readonly ILogger<ShoppingBasketService> _logger;
    private readonly IShoppingBasketRepository _shoppingBasketRepository;
    private readonly IProductCatalogClient _productCatalog;
    
    public ShoppingBasketService(ILogger<ShoppingBasketService> logger, IShoppingBasketRepository shoppingBasketRepository, IProductCatalogClient productCatalog)
    {
        _logger = logger;
        _shoppingBasketRepository = shoppingBasketRepository;
        _productCatalog = productCatalog;
    }

    public async Task<BasketDto?> GetBasketAsync(Guid id)
    {
        _logger.LogInformation("Fetching basket with id {BasketId}", id);

        var basket = await _shoppingBasketRepository.GetAsync(id.ToString());

        if (basket == null)
        {
            _logger.LogWarning("Basket not found: {BasketId}", id);
            return null;
        }

        _logger.LogInformation("Basket found: {BasketId} with {ItemCount} items", id, basket.Items?.Count ?? 0);

        return new BasketDto
        {
            Id = basket.Id,
            Items = basket.Items.Select(i => new BasketItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }
}
