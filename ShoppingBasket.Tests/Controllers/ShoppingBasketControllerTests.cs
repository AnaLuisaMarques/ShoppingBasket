using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingBasket.API.Controllers;
using ShoppingBasket.API.Interfaces;
using ShoppingBasket.Contracts.DTOs;
using Xunit;

namespace ShoppingBasket.Tests.Controllers;

public class ShoppingBasketControllerTests
{
    [Fact]
    public async Task CreateShoppingBasket_ReturnsCreated_WithLocation()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var input = new BasketDto { Id = Guid.Empty, Items = new List<BasketItemDto>() };
        var created = new BasketDto { Id = Guid.NewGuid(), Items = new List<BasketItemDto>() };

        mock.Setup(s => s.CreateBasketAsync(input)).ReturnsAsync(created);

        var controller = new ShoppingBasketController(mock.Object);

        // Act
        var result = await controller.CreateShoppingBasket(input) as CreatedAtActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(201, result.StatusCode);
        var returned = Assert.IsType<BasketDto>(result.Value);
        Assert.Equal(created.Id, returned.Id);
        // verify route values contain the id for the created resource
        Assert.NotNull(result.RouteValues);
        Assert.True(result.RouteValues.ContainsKey("id"));
        Assert.Equal(created.Id.ToString(), result.RouteValues["id"]?.ToString());
    }

    [Fact]
    public async Task GetShoppingBasketById_ReturnsBadRequest_OnEmptyId()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var controller = new ShoppingBasketController(mock.Object);

        // Act
        var result = await controller.GetShoppingBasketById(Guid.Empty, default);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetShoppingBasketById_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var id = Guid.NewGuid();
        mock.Setup(s => s.GetBasketAsync(id)).ReturnsAsync((BasketDto?)null);
        var controller = new ShoppingBasketController(mock.Object);

        // Act
        var result = await controller.GetShoppingBasketById(id, default);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetShoppingBasketById_ReturnsOk_WhenFound()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var id = Guid.NewGuid();
        var dto = new BasketDto { Id = id, Items = new List<BasketItemDto>() };
        mock.Setup(s => s.GetBasketAsync(id)).ReturnsAsync(dto);
        var controller = new ShoppingBasketController(mock.Object);

        // Act
        var result = await controller.GetShoppingBasketById(id, default) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var returned = Assert.IsType<BasketDto>(result.Value);
        Assert.Equal(id, returned.Id);
    }

    [Fact]
    public async Task DeleteShoppingBasket_ReturnsBadRequest_OnEmptyId()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var controller = new ShoppingBasketController(mock.Object);

        // Act
        var result = await controller.DeleteShoppingBasket(Guid.Empty);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteShoppingBasket_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var id = Guid.NewGuid();
        mock.Setup(s => s.DeleteBasketAsync(id)).ReturnsAsync(false);
        var controller = new ShoppingBasketController(mock.Object);

        // Act
        var result = await controller.DeleteShoppingBasket(id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddItemsToBasket_ReturnsBadRequest_OnInvalid()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var controller = new ShoppingBasketController(mock.Object);

        // Act - empty id
        var bad = await controller.AddItemsToBasket(Guid.Empty, new[] { new AddItemRequest { ProductId = "1", Quantity = 1 } });
        Assert.IsType<BadRequestObjectResult>(bad);

        // Act - empty items
        bad = await controller.AddItemsToBasket(Guid.NewGuid(), Array.Empty<AddItemRequest>());
        Assert.IsType<BadRequestObjectResult>(bad);
    }

    [Fact]
    public async Task AddItemsToBasket_ReturnsNotFound_WhenServiceReturnsNull()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var id = Guid.NewGuid();
        mock.Setup(s => s.AddItemsAsync(id, It.IsAny<IEnumerable<AddItemRequest>>())).ReturnsAsync((BasketDto?)null);
        var controller = new ShoppingBasketController(mock.Object);

        // Act
        var result = await controller.AddItemsToBasket(id, new[] { new AddItemRequest { ProductId = "1", Quantity = 1 } });

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateItemsInBasket_ReturnsBadRequest_OnInvalid()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var controller = new ShoppingBasketController(mock.Object);

        // Act - empty id
        var bad = await controller.UpdateItemsInBasket(Guid.Empty, new[] { new AddItemRequest { ProductId = "1", Quantity = 1 } });
        Assert.IsType<BadRequestObjectResult>(bad);

        // Act - empty items
        bad = await controller.UpdateItemsInBasket(Guid.NewGuid(), Array.Empty<AddItemRequest>());
        Assert.IsType<BadRequestObjectResult>(bad);
    }

    [Fact]
    public async Task UpdateItemsInBasket_ReturnsNotFound_WhenServiceReturnsNull()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var id = Guid.NewGuid();
        mock.Setup(s => s.UpdateItemsAsync(id, It.IsAny<IEnumerable<AddItemRequest>>())).ReturnsAsync((BasketDto?)null);
        var controller = new ShoppingBasketController(mock.Object);

        // Act
        var result = await controller.UpdateItemsInBasket(id, new[] { new AddItemRequest { ProductId = "1", Quantity = 1 } });

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RemoveItemsFromBasket_ReturnsBadRequest_OnInvalid()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var controller = new ShoppingBasketController(mock.Object);

        // Act - empty id
        var bad = await controller.RemoveItemsFromBasket(Guid.Empty, new[] { new DeleteItemRequest { ProductId = "1" } });
        Assert.IsType<BadRequestObjectResult>(bad);

        // Act - empty items
        bad = await controller.RemoveItemsFromBasket(Guid.NewGuid(), Array.Empty<DeleteItemRequest>());
        Assert.IsType<BadRequestObjectResult>(bad);
    }

    [Fact]
    public async Task RemoveItemsFromBasket_ReturnsNotFound_WhenServiceReturnsNull()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var id = Guid.NewGuid();
        mock.Setup(s => s.RemoveItemsAsync(id, It.IsAny<IEnumerable<DeleteItemRequest>>())).ReturnsAsync((BasketDto?)null);
        var controller = new ShoppingBasketController(mock.Object);

        // Act
        var result = await controller.RemoveItemsFromBasket(id, new[] { new DeleteItemRequest { ProductId = "1" } });

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteShoppingBasket_ReturnsNoContent_WhenDeleted()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var id = Guid.NewGuid();
        mock.Setup(s => s.DeleteBasketAsync(id)).ReturnsAsync(true);

        var controller = new ShoppingBasketController(mock.Object);

        // Act
        var result = await controller.DeleteShoppingBasket(id);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task AddItemsToBasket_ReturnsOkWithUpdatedBasket()
    {
        // Arrange
        var mock = new Mock<IShoppingBasketService>();
        var id = Guid.NewGuid();
        var items = new[] { new AddItemRequest { ProductId = "1", Quantity = 2 } };
        var updated = new BasketDto { Id = id, Items = new List<BasketItemDto> { new BasketItemDto { ProductId = "1", ProductName = "P", Quantity = 2, UnitPrice = 1m } } };

        mock.Setup(s => s.AddItemsAsync(id, It.IsAny<IEnumerable<AddItemRequest>>())).ReturnsAsync(updated);

        var controller = new ShoppingBasketController(mock.Object);

        // Act
        var result = await controller.AddItemsToBasket(id, items) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var returned = Assert.IsType<BasketDto>(result.Value);
        Assert.Equal(id, returned.Id);
        Assert.Single(returned.Items);
    }
}
