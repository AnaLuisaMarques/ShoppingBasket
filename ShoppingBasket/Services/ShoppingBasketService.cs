using ShoppingBasket.API.Interfaces;

namespace ShoppingBasket.API.Services;

public class ShoppingBasketService : IShoppingBasketService
{
    private readonly IProductCatalogClient _catalog;

    public ShoppingBasketService(IProductCatalogClient catalog)
    {
        _catalog = catalog;
    }
}
