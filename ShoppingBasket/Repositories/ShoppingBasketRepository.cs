using ShoppingBasket.API.Interfaces;
using ShoppingBasket.Contracts.Models;

namespace ShoppingBasket.API.Repositories
{

    public class ShoppingBasketRepository : IShoppingBasketRepository
    {
        private readonly Dictionary<string, Basket> _store = new();

        public Task<Basket?> GetAsync(string id)
        {
            _store.TryGetValue(id, out var basket);
            return Task.FromResult(basket);
        }

        public Task SaveAsync(Basket basket)
        {
            _store[basket.Id] = basket;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _store.Remove(id);
            return Task.CompletedTask;
        }
    }
}