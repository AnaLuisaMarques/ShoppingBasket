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

    public async Task<BasketDto?> AddItemsAsync(Guid basketId, IEnumerable<AddItemRequest> items)
    {
        if (basketId == Guid.Empty) return null;
        if (items == null) return null;

        var existing = await _shoppingBasketRepository.GetAsync(basketId.ToString());

        if (existing == null) return null;

        foreach (var req in items)
        {
            if (string.IsNullOrEmpty(req.ProductId) || req.Quantity <= 0) continue;

            var product = await _productCatalog.GetProductAsync(req.ProductId);
            var unitPrice = product?.Price ?? 0m;
            var productName = product?.Name ?? string.Empty;

            var existingItem = existing.Items?.FirstOrDefault(i => i.ProductId == req.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity = req.Quantity;

                if (product != null)
                {
                    existingItem.UnitPrice = unitPrice;
                    existingItem.ProductName = productName;
                }
            }
            else
            {
                if (existing.Items == null) existing.Items = new List<BasketItem>();

                existing.Items.Add(new BasketItem
                {
                    ProductId = req.ProductId,
                    ProductName = productName,
                    Quantity = req.Quantity,
                    UnitPrice = unitPrice
                });
            }
        }

        await _shoppingBasketRepository.SaveAsync(existing);

        return new BasketDto
        {
            Id = existing.Id,
            Items = existing.Items?.Select(i => new BasketItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList() ?? new List<BasketItemDto>()
        };
    }

    public async Task<BasketDto?> UpdateItemsAsync(Guid basketId, IEnumerable<AddItemRequest> items)
    {
        if (basketId == Guid.Empty) return null;
        if (items == null) return null;

        var existing = await _shoppingBasketRepository.GetAsync(basketId.ToString());

        if (existing == null) return null;

        foreach (var req in items)
        {
            if (string.IsNullOrEmpty(req.ProductId)) continue;

            var existingItem = existing.Items.FirstOrDefault(i => i.ProductId == req.ProductId);

            if (existingItem == null) continue; // only update items that already exist

            if (req.Quantity <= 0)
            {
                // remove item
                existing.Items.Remove(existingItem);
                continue;
            }

            // set to requested quantity
            existingItem.Quantity = req.Quantity;

            var product = await _productCatalog.GetProductAsync(req.ProductId);

            if (product != null)
            {
                existingItem.UnitPrice = product.Price;
                existingItem.ProductName = product.Name;
            }
        }

        await _shoppingBasketRepository.SaveAsync(existing);

        return new BasketDto
        {
            Id = existing.Id,
            Items = existing.Items.Select(i => new BasketItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }

    public async Task<BasketDto?> RemoveItemsAsync(Guid basketId, IEnumerable<DeleteItemRequest> items)
    {
        if (basketId == Guid.Empty) return null;
        if (items == null) return null;

        var existing = await _shoppingBasketRepository.GetAsync(basketId.ToString());

        if (existing == null) return null;

        var idsToRemove = items.Where(i => !string.IsNullOrEmpty(i.ProductId)).Select(i => i.ProductId).ToHashSet();

        if (idsToRemove.Count == 0) 
            return new BasketDto 
            { 
                Id = existing.Id, 
                Items = existing.Items.Select(i => new BasketItemDto 
                    { 
                        ProductId = i.ProductId, 
                        ProductName = i.ProductName, 
                        Quantity = i.Quantity, 
                        UnitPrice = i.UnitPrice 
                    }
                ).ToList() 
            };

        existing.Items.RemoveAll(i => idsToRemove.Contains(i.ProductId));

        await _shoppingBasketRepository.SaveAsync(existing);

        return new BasketDto
        {
            Id = existing.Id,
            Items = existing.Items.Select(i => new BasketItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }
}
