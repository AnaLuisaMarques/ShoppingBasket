using ShoppingBasket.Contracts.Models;
    
namespace ShoppingBasket.API.Interfaces;

public interface IShoppingBasketRepository
{
    Task<Basket?> GetAsync(string id);
    Task SaveAsync(Basket basket);
    Task DeleteAsync(string id);
}
