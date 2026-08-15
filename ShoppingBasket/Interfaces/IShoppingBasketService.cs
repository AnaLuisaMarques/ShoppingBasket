namespace ShoppingBasket.API.Interfaces;

using ShoppingBasket.Contracts.DTOs;

public interface IShoppingBasketService
{
    Task<BasketDto?> GetBasketAsync(Guid id);
}
