using Microsoft.Extensions.Logging;
using Moq;
using ShoppingBasket.API.Interfaces;
using ShoppingBasket.API.Services;
using ShoppingBasket.Contracts.DTOs;
using ShoppingBasket.Contracts.Models;
using Xunit;

namespace ShoppingBasket.Tests.Services
{
    public class ShoppingBasketServiceTests
    {
        [Fact]
        public async Task GetBasketAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            var repoMock = new Mock<IShoppingBasketRepository>();
            repoMock.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync((Basket?)null);
            var productMock = new Mock<IProductCatalogClient>();
            var logger = new Mock<ILogger<ShoppingBasketService>>();
            var svc = new ShoppingBasketService(logger.Object, repoMock.Object, productMock.Object);

            // Act
            var result = await svc.GetBasketAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBasketAsync_MapsModelToDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var model = new Basket
            {
                Id = id,
                Items = new List<BasketItem>
                {
                    new BasketItem { ProductId = "1", ProductName = "P1", Quantity = 2, UnitPrice = 1m }
                }
            };

            var repoMock = new Mock<IShoppingBasketRepository>();
            repoMock.Setup(r => r.GetAsync(id.ToString())).ReturnsAsync(model);
            var productMock = new Mock<IProductCatalogClient>();
            var logger = new Mock<ILogger<ShoppingBasketService>>();
            var svc = new ShoppingBasketService(logger.Object, repoMock.Object, productMock.Object);

            // Act
            var dto = await svc.GetBasketAsync(id);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(id, dto!.Id);
            Assert.Single(dto.Items);
            Assert.Equal("1", dto.Items[0].ProductId);
        }

        [Fact]
        public async Task CreateBasketAsync_SavesAndReturnsDto_UsesProductCatalogWhenAvailable()
        {
            // Arrange
            var id = Guid.Empty; // let service generate id
            var input = new BasketDto
            {
                Id = Guid.Empty,
                Items = new List<BasketItemDto>
                {
                    new BasketItemDto { ProductId = "1", ProductName = "X", Quantity = 1, UnitPrice = 5m },
                    new BasketItemDto { ProductId = "2", ProductName = "Y", Quantity = 2, UnitPrice = 7m }
                }
            };

            var productMock = new Mock<IProductCatalogClient>();
            productMock.Setup(p => p.GetProductAsync("1")).ReturnsAsync(new ProductDto { Id = 1, Name = "Prod1", Price = 3m });
            productMock.Setup(p => p.GetProductAsync("2")).ReturnsAsync((ProductDto?)null);

            Basket? saved = null;
            var repoMock = new Mock<IShoppingBasketRepository>();
            repoMock.Setup(r => r.SaveAsync(It.IsAny<Basket>())).Callback<Basket>(b => saved = b).Returns(Task.CompletedTask);

            var logger = new Mock<ILogger<ShoppingBasketService>>();
            var svc = new ShoppingBasketService(logger.Object, repoMock.Object, productMock.Object);

            // Act
            var created = await svc.CreateBasketAsync(input);

            // Assert
            Assert.NotNull(created);
            Assert.NotEqual(Guid.Empty, created.Id);
            // verify repository saved the basket
            repoMock.Verify(r => r.SaveAsync(It.IsAny<Basket>()), Times.Once);
            Assert.NotNull(saved);
            // product 1 price should come from product catalog (3m), product 2 uses passed unit price (7m)
            var savedItem1 = saved!.Items.First(i => i.ProductId == "1");
            var savedItem2 = saved!.Items.First(i => i.ProductId == "2");
            Assert.Equal(3m, savedItem1.UnitPrice);
            Assert.Equal(7m, savedItem2.UnitPrice);
        }

        [Fact]
        public async Task DeleteBasketAsync_ReturnsFalse_WhenNotFound()
        {
            // Arrange
            var repoMock = new Mock<IShoppingBasketRepository>();
            repoMock.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync((Basket?)null);
            var productMock = new Mock<IProductCatalogClient>();
            var logger = new Mock<ILogger<ShoppingBasketService>>();
            var svc = new ShoppingBasketService(logger.Object, repoMock.Object, productMock.Object);

            // Act
            var result = await svc.DeleteBasketAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteBasketAsync_DeletesAndReturnsTrue_WhenExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var model = new Basket { Id = id };
            var repoMock = new Mock<IShoppingBasketRepository>();
            repoMock.Setup(r => r.GetAsync(id.ToString())).ReturnsAsync(model);
            repoMock.Setup(r => r.DeleteAsync(id.ToString())).Returns(Task.CompletedTask).Verifiable();
            var productMock = new Mock<IProductCatalogClient>();
            var logger = new Mock<ILogger<ShoppingBasketService>>();
            var svc = new ShoppingBasketService(logger.Object, repoMock.Object, productMock.Object);

            // Act
            var result = await svc.DeleteBasketAsync(id);

            // Assert
            Assert.True(result);
            repoMock.Verify(r => r.DeleteAsync(id.ToString()), Times.Once);
        }

        [Fact]
        public async Task AddItemsAsync_AddsNewItemAndUpdatesExisting()
        {
            // Arrange
            var id = Guid.NewGuid();
            var existing = new Basket
            {
                Id = id,
                Items = new List<BasketItem>
                {
                    new BasketItem { ProductId = "1", ProductName = "P1", Quantity = 1, UnitPrice = 1m }
                }
            };

            var repoMock = new Mock<IShoppingBasketRepository>();
            repoMock.Setup(r => r.GetAsync(id.ToString())).ReturnsAsync(existing);
            Basket? saved = null;
            repoMock.Setup(r => r.SaveAsync(It.IsAny<Basket>())).Callback<Basket>(b => saved = b).Returns(Task.CompletedTask);

            var productMock = new Mock<IProductCatalogClient>();
            productMock.Setup(p => p.GetProductAsync("1")).ReturnsAsync(new ProductDto { Id = 1, Name = "P1", Price = 2m });
            productMock.Setup(p => p.GetProductAsync("2")).ReturnsAsync(new ProductDto { Id = 2, Name = "P2", Price = 5m });

            var svc = new ShoppingBasketService(new Mock<ILogger<ShoppingBasketService>>().Object, repoMock.Object, productMock.Object);

            var items = new[] { new AddItemRequest { ProductId = "1", Quantity = 2 }, new AddItemRequest { ProductId = "2", Quantity = 3 } };

            // Act
            var updated = await svc.AddItemsAsync(id, items);

            // Assert
            Assert.NotNull(updated);
            var dto = updated!;
            // product 1 existed and quantity replaced by requested value (2), and price updated to product price (2m)
            var item1 = dto.Items.First(i => i.ProductId == "1");
            Assert.Equal(2, item1.Quantity);
            Assert.Equal(2m, item1.UnitPrice);
            // product 2 added
            var item2 = dto.Items.First(i => i.ProductId == "2");
            Assert.Equal(3, item2.Quantity);
            Assert.Equal(5m, item2.UnitPrice);
            // verify repository saved the modified model and it contains expected state
            Assert.NotNull(saved);
            var savedItem1 = saved!.Items.First(i => i.ProductId == "1");
            var savedItem2 = saved!.Items.First(i => i.ProductId == "2");
            Assert.Equal(2, savedItem1.Quantity);
            Assert.Equal(5m, savedItem2.UnitPrice);
            repoMock.Verify(r => r.SaveAsync(It.IsAny<Basket>()), Times.Once);
        }

        [Fact]
        public async Task UpdateItemsAsync_UpdatesQuantity_AndRemovesWhenZero()
        {
            // Arrange
            var id = Guid.NewGuid();
            var existing = new Basket
            {
                Id = id,
                Items = new List<BasketItem>
                {
                    new BasketItem { ProductId = "1", ProductName = "P1", Quantity = 5, UnitPrice = 1m },
                    new BasketItem { ProductId = "2", ProductName = "P2", Quantity = 2, UnitPrice = 2m }
                }
            };

            var repoMock = new Mock<IShoppingBasketRepository>();
            repoMock.Setup(r => r.GetAsync(id.ToString())).ReturnsAsync(existing);
            repoMock.Setup(r => r.SaveAsync(It.IsAny<Basket>())).Returns(Task.CompletedTask).Verifiable();

            var productMock = new Mock<IProductCatalogClient>();
            productMock.Setup(p => p.GetProductAsync("1")).ReturnsAsync(new ProductDto { Id = 1, Name = "P1", Price = 10m });

            var svc = new ShoppingBasketService(new Mock<ILogger<ShoppingBasketService>>().Object, repoMock.Object, productMock.Object);

            var updates = new[] { new AddItemRequest { ProductId = "1", Quantity = 3 }, new AddItemRequest { ProductId = "2", Quantity = 0 } };

            // Act
            var updated = await svc.UpdateItemsAsync(id, updates);

            // Assert
            Assert.NotNull(updated);
            var dto = updated!;
            Assert.Equal(1, dto.Items.Count); // product 2 removed
            var item1 = dto.Items.First(i => i.ProductId == "1");
            Assert.Equal(3, item1.Quantity);
            // unit price refreshed from product catalog
            Assert.Equal(10m, item1.UnitPrice);
            repoMock.Verify(r => r.SaveAsync(It.IsAny<Basket>()), Times.Once);
        }

        [Fact]
        public async Task RemoveItemsAsync_RemovesSpecifiedItems()
        {
            // Arrange
            var id = Guid.NewGuid();
            var existing = new Basket
            {
                Id = id,
                Items = new List<BasketItem>
                {
                    new BasketItem { ProductId = "1", ProductName = "P1", Quantity = 1, UnitPrice = 1m },
                    new BasketItem { ProductId = "2", ProductName = "P2", Quantity = 1, UnitPrice = 2m }
                }
            };

            var repoMock = new Mock<IShoppingBasketRepository>();
            repoMock.Setup(r => r.GetAsync(id.ToString())).ReturnsAsync(existing);
            repoMock.Setup(r => r.SaveAsync(It.IsAny<Basket>())).Returns(Task.CompletedTask).Verifiable();

            var productMock = new Mock<IProductCatalogClient>();

            var svc = new ShoppingBasketService(new Mock<ILogger<ShoppingBasketService>>().Object, repoMock.Object, productMock.Object);

            var toRemove = new[] { new DeleteItemRequest { ProductId = "1" } };

            // Act
            var updated = await svc.RemoveItemsAsync(id, toRemove);

            // Assert
            Assert.NotNull(updated);
            var dto = updated!;
            Assert.Single(dto.Items);
            Assert.DoesNotContain(dto.Items, i => i.ProductId == "1");
            repoMock.Verify(r => r.SaveAsync(It.IsAny<Basket>()), Times.Once);
        }
    }
}
