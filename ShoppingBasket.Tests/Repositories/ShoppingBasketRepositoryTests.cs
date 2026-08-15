using ShoppingBasket.API.Repositories;
using ShoppingBasket.Contracts.Models;
using Xunit;

namespace ShoppingBasket.Tests.Repositories
{
    public class ShoppingBasketRepositoryTests
    {
        [Fact]
        public async Task SaveAsync_Then_GetAsync_ReturnsSavedBasket()
        {
            // Arrange
            var repo = new ShoppingBasketRepository();
            var basket = new Basket
            {
                Id = Guid.NewGuid(),
                Items = new List<BasketItem>
                {
                    new BasketItem { ProductId = "1", ProductName = "P1", Quantity = 2, UnitPrice = 1m }
                }
            };

            // Act
            await repo.SaveAsync(basket);
            var fetched = await repo.GetAsync(basket.Id.ToString());

            // Assert
            Assert.NotNull(fetched);
            Assert.Equal(basket.Id, fetched!.Id);
            Assert.Single(fetched.Items);
            Assert.Equal("1", fetched.Items[0].ProductId);
        }

        [Fact]
        public async Task DeleteAsync_RemovesBasket()
        {
            // Arrange
            var repo = new ShoppingBasketRepository();
            var basket = new Basket { Id = Guid.NewGuid() };
            await repo.SaveAsync(basket);

            // Act
            await repo.DeleteAsync(basket.Id.ToString());
            var fetched = await repo.GetAsync(basket.Id.ToString());

            // Assert
            Assert.Null(fetched);
        }

        [Fact]
        public async Task SaveAsync_OverwriteExisting_UpdatesBasket()
        {
            // Arrange
            var repo = new ShoppingBasketRepository();
            var id = Guid.NewGuid();
            var basket1 = new Basket { Id = id, Items = new List<BasketItem> { new BasketItem { ProductId = "1", Quantity = 1, ProductName = "A", UnitPrice = 1m } } };
            var basket2 = new Basket { Id = id, Items = new List<BasketItem> { new BasketItem { ProductId = "2", Quantity = 3, ProductName = "B", UnitPrice = 2m } } };

            // Act
            await repo.SaveAsync(basket1);
            await repo.SaveAsync(basket2);
            var fetched = await repo.GetAsync(id.ToString());

            // Assert
            Assert.NotNull(fetched);
            Assert.Equal(id, fetched!.Id);
            Assert.Single(fetched.Items);
            Assert.Equal("2", fetched.Items[0].ProductId);
            Assert.Equal(3, fetched.Items[0].Quantity);
        }
    }
}
