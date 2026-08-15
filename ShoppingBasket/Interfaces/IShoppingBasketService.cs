namespace ShoppingBasket.API.Interfaces;

using ShoppingBasket.Contracts.DTOs;

public interface IShoppingBasketService
{
    Task<BasketDto?> GetBasketAsync(Guid id);
    Task<BasketDto> CreateBasketAsync(BasketDto basket);
    Task<bool> DeleteBasketAsync(Guid id);
    Task<BasketDto?> AddItemsAsync(Guid basketId, IEnumerable<AddItemRequest> items);
}
