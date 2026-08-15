using ShoppingBasket.API.Interfaces;
using ShoppingBasket.Contracts.DTOs;
using ShoppingBasket.Contracts.Models;  

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

    public async Task<BasketDto> CreateBasketAsync(BasketDto basket)
    {
        if (basket == null) throw new ArgumentNullException(nameof(basket));

        var id = basket.Id == Guid.Empty ? Guid.NewGuid() : basket.Id;

        var model = new Basket
        {
            Id = id,
            Items = new List<BasketItem>()
        };

        foreach (var item in basket.Items ?? Enumerable.Empty<BasketItemDto>())
        {
            var product = await _productCatalog.GetProductAsync(item.ProductId);
            var unitPrice = product?.Price ?? item.UnitPrice;
            var productName = product?.Name ?? item.ProductName;

            model.Items.Add(new BasketItem
            {
                ProductId = item.ProductId,
                ProductName = productName,
                Quantity = item.Quantity,
                UnitPrice = unitPrice
            });
        }

        await _shoppingBasketRepository.SaveAsync(model);

        return new BasketDto
        {
            Id = model.Id,
            Items = model.Items.Select(i => new BasketItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }

    public async Task<bool> DeleteBasketAsync(Guid id)
    {
        if (id == Guid.Empty) return false;

        var existing = await _shoppingBasketRepository.GetAsync(id.ToString());

        if (existing == null) return false;

        await _shoppingBasketRepository.DeleteAsync(id.ToString());

        return true;
    }
}
