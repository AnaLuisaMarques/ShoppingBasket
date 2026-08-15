using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingBasket.API.Controllers;
using ShoppingBasket.API.Interfaces;
using ShoppingBasket.Contracts.DTOs;
using Xunit;

namespace ShoppingBasket.Tests.Controllers;

public class ProductsControllerTests
{
    [Fact]
    public async Task GetCheapest_ReturnsOkWithProducts()
    {
        // Arrange
        var mock = new Mock<IProductCatalogClient>();
        var products = Enumerable.Range(1, 5).Select(i => new ProductDto
        {
            Id = i,
            Name = $"p{i}",
            Price = i,
            Size = 1,
            Stars = 5
        }).ToList();

        mock.Setup(m => m.GetCheapestProductsAsync(5)).ReturnsAsync(products);

        var controller = new ProductsController(mock.Object);

        // Act
        var actionResult = await controller.GetCheapestProducts(5) as OkObjectResult;

        // Assert
        Assert.NotNull(actionResult);
        var value = Assert.IsType<List<ProductDto>>(actionResult.Value);
        Assert.Equal(5, value.Count);
    }

    [Fact]
    public async Task GetTop100Products_ReturnsOkWithProducts()
    {
        // Arrange
        var mock = new Mock<IProductCatalogClient>();
        var products = Enumerable.Range(1, 3).Select(i => new ProductDto { Id = i, Name = $"p{i}", Price = i, Size = 1, Stars = 5 }).ToList();
        mock.Setup(m => m.GetTop100ProductsAsync()).ReturnsAsync(products);
        var controller = new ProductsController(mock.Object);

        // Act
        var actionResult = await controller.GetTop100Products() as OkObjectResult;

        // Assert
        Assert.NotNull(actionResult);
        var value = Assert.IsType<List<ProductDto>>(actionResult.Value);
        Assert.Equal(3, value.Count);
    }

    [Fact]
    public async Task GetPagedProducts_ReturnsOk_WithPagingEnvelope()
    {
        // Arrange
        var mock = new Mock<IProductCatalogClient>();
        var items = Enumerable.Range(1, 5).Select(i => new ProductDto { Id = i, Name = $"p{i}", Price = i, Size = 1, Stars = 5 }).ToList();
        var total = 12;
        mock.Setup(m => m.GetPagedProductsAsync(2, 5)).ReturnsAsync((items, total));
        var controller = new ProductsController(mock.Object);

        // Act
        var actionResult = await controller.GetPagedProducts(2, 5) as OkObjectResult;

        // Assert
        Assert.NotNull(actionResult);
        var value = actionResult.Value!;
        var t = value.GetType();
        var itemsProp = t.GetProperty("items");
        var pageNumberProp = t.GetProperty("pageNumber");
        var pageSizeProp = t.GetProperty("pageSize");
        var totalCountProp = t.GetProperty("totalCount");
        var totalPagesProp = t.GetProperty("totalPages");

        Assert.NotNull(itemsProp);
        Assert.NotNull(pageNumberProp);
        Assert.NotNull(pageSizeProp);
        Assert.NotNull(totalCountProp);
        Assert.NotNull(totalPagesProp);

        var returnedItems = Assert.IsType<List<ProductDto>>(itemsProp.GetValue(value));
        Assert.Equal(5, returnedItems.Count);
        Assert.Equal(2, (int)pageNumberProp.GetValue(value)!);
        Assert.Equal(5, (int)pageSizeProp.GetValue(value)!);
        Assert.Equal(total, (int)totalCountProp.GetValue(value)!);
        Assert.Equal((int)Math.Ceiling(total / (double)5), (int)totalPagesProp.GetValue(value)!);
    }

    [Fact]
    public async Task GetPagedProducts_ReturnsBadRequest_OnInvalidParams()
    {
        // Arrange
        var mock = new Mock<IProductCatalogClient>();
        var controller = new ProductsController(mock.Object);

        // Act / Assert
        var badPage = await controller.GetPagedProducts(0, 10);
        Assert.IsType<BadRequestObjectResult>(badPage);

        var badPageSize = await controller.GetPagedProducts(1, 0);
        Assert.IsType<BadRequestObjectResult>(badPageSize);

        var tooLarge = await controller.GetPagedProducts(1, 2000);
        Assert.IsType<BadRequestObjectResult>(tooLarge);
    }

    [Fact]
    public async Task GetCheapestProducts_ReturnsBadRequest_WhenNumberInvalid()
    {
        // Arrange
        var mock = new Mock<IProductCatalogClient>();
        var controller = new ProductsController(mock.Object);

        // Act
        var result = await controller.GetCheapestProducts(0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetCheapestProducts_DefaultsToTen_WhenNoParam()
    {
        // Arrange
        var mock = new Mock<IProductCatalogClient>();
        var products = Enumerable.Range(1, 10).Select(i => new ProductDto { Id = i, Name = $"p{i}", Price = i, Size = 1, Stars = 5 }).ToList();
        mock.Setup(m => m.GetCheapestProductsAsync(10)).ReturnsAsync(products);
        var controller = new ProductsController(mock.Object);

        // Act
        var actionResult = await controller.GetCheapestProducts() as OkObjectResult;

        // Assert
        Assert.NotNull(actionResult);
        var value = Assert.IsType<List<ProductDto>>(actionResult.Value);
        Assert.Equal(10, value.Count);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenProductMissing()
    {
        // Arrange
        var mock = new Mock<IProductCatalogClient>();
        mock.Setup(m => m.GetProductAsync("missing")).ReturnsAsync((ProductDto?)null);

        var controller = new ProductsController(mock.Object);

        // Act
        var result = await controller.GetProductById("missing");

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsOk_WhenProductExists()
    {
        // Arrange
        var mock = new Mock<IProductCatalogClient>();
        var product = new ProductDto { Id = 1, Name = "A", Price = 1m, Size = 1, Stars = 5 };
        mock.Setup(m => m.GetProductAsync("1")).ReturnsAsync(product);

        var controller = new ProductsController(mock.Object);

        // Act
        var result = await controller.GetProductById("1") as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var returned = Assert.IsType<ProductDto>(result.Value);
        Assert.Equal(product.Id, returned.Id);
    }
}
