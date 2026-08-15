using ShoppingBasket.Contracts.DTOs;

namespace ShoppingBasket.API.Interfaces;

public interface IProductCatalogClient
{
    Task<ProductDto?> GetProductAsync(string productId);
    Task<IList<ProductDto>> GetTop100ProductsAsync();
    Task<(IList<ProductDto> Items, int TotalCount)> GetProductsAsync(int pageNumber, int pageSize);
}
