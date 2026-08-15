using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingBasket.API.Interfaces;
using ShoppingBasket.API.Services;
using ShoppingBasket.Contracts.DTOs;
using System.Net;
using System.Text.Json;
using Xunit;

namespace ShoppingBasket.Tests.Services
{
    internal class ProductCatalogFakeHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public ProductCatalogFakeHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    internal class CountingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public int CallCount { get; private set; }
        public CountingHandler(HttpResponseMessage response)
        {
            _response = response;
            CallCount = 0;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_response);
        }
    }

    public class ProductCatalogClientTests
    {
        private static HttpClient CreateHttpClientWithProducts(IEnumerable<ProductDto> products)
        {
            var json = JsonSerializer.Serialize(products);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
            var handler = new ProductCatalogFakeHandler(response);
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };
            return client;
        }

        [Fact]
        public async Task GetCheapestProductsAsync_ReturnsEmpty_WhenCountIsZero()
        {
            // Arrange
            var products = new List<ProductDto>
            {
                new ProductDto { Id = 1, Name = "A", Price = 10m },
                new ProductDto { Id = 2, Name = "B", Price = 5m }
            };
            var client = CreateHttpClientWithProducts(products);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Mock<ILogger<ProductCatalogClient>>().Object;
            var tokenProv = new Mock<ITokenProvider>();
            tokenProv.Setup(t => t.EnsureTokenAsync(default)).Returns(Task.CompletedTask);
            tokenProv.SetupGet(t => t.CurrentToken).Returns(string.Empty);

            var svc = new ProductCatalogClient(client, cache, logger, tokenProv.Object);

            // Act
            var cheapest = await svc.GetCheapestProductsAsync(0);

            // Assert
            Assert.NotNull(cheapest);
            Assert.Empty(cheapest);
        }

        [Fact]
        public async Task GetAllProductsCached_UsesCache_ToAvoidMultipleHttpCalls()
        {
            // Arrange
            var products = new List<ProductDto>
            {
                new ProductDto { Id = 1, Name = "A", Price = 1m },
                new ProductDto { Id = 2, Name = "B", Price = 2m }
            };
            var json = JsonSerializer.Serialize(products);
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            var handler = new CountingHandler(response);
            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Mock<ILogger<ProductCatalogClient>>().Object;
            var tokenProv = new Mock<ITokenProvider>();
            tokenProv.Setup(t => t.EnsureTokenAsync(default)).Returns(Task.CompletedTask);
            tokenProv.SetupGet(t => t.CurrentToken).Returns(string.Empty);

            var svc = new ProductCatalogClient(client, cache, logger, tokenProv.Object);

            // Act
            var first = await svc.GetTop100ProductsAsync();
            var second = await svc.GetTop100ProductsAsync();

            // Assert - handler should have been called only once because results were cached
            Assert.Equal(1, handler.CallCount);
            Assert.Equal(2, first.Count);
            Assert.Equal(2, second.Count);
        }

        [Fact]
        public async Task GetProductAsync_ReturnsProduct_WhenExists()
        {
            // Arrange
            var products = new List<ProductDto>
            {
                new ProductDto { Id = 1, Name = "A", Price = 5m, Size = 1, Stars = 5 },
                new ProductDto { Id = 2, Name = "B", Price = 3m, Size = 1, Stars = 4 }
            };
            var client = CreateHttpClientWithProducts(products);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Mock<ILogger<ProductCatalogClient>>().Object;
            var tokenProv = new Mock<ITokenProvider>();
            tokenProv.Setup(t => t.EnsureTokenAsync(default)).Returns(Task.CompletedTask);
            tokenProv.SetupGet(t => t.CurrentToken).Returns(string.Empty);

            var svc = new ProductCatalogClient(client, cache, logger, tokenProv.Object);

            // Act
            var result = await svc.GetProductAsync("1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        [Fact]
        public async Task GetTop100ProductsAsync_ReturnsAtMost100()
        {
            // Arrange
            var products = new List<ProductDto>();
            for (int i = 1; i <= 150; i++) products.Add(new ProductDto { Id = i, Name = $"p{i}", Price = i, Size = 1, Stars = 5 });
            var client = CreateHttpClientWithProducts(products);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Mock<ILogger<ProductCatalogClient>>().Object;
            var tokenProv = new Mock<ITokenProvider>();
            tokenProv.Setup(t => t.EnsureTokenAsync(default)).Returns(Task.CompletedTask);
            tokenProv.SetupGet(t => t.CurrentToken).Returns(string.Empty);

            var svc = new ProductCatalogClient(client, cache, logger, tokenProv.Object);

            // Act
            var top = await svc.GetTop100ProductsAsync();

            // Assert
            Assert.NotNull(top);
            Assert.True(top.Count <= 100);
            Assert.Equal(100, top.Count);
        }

        [Fact]
        public async Task GetPagedProductsAsync_ReturnsOrderedPage()
        {
            // Arrange
            var products = new List<ProductDto>
            {
                new ProductDto { Id = 1, Name = "A", Price = 10m },
                new ProductDto { Id = 2, Name = "B", Price = 5m },
                new ProductDto { Id = 3, Name = "C", Price = 7m },
                new ProductDto { Id = 4, Name = "D", Price = 2m }
            };
            var client = CreateHttpClientWithProducts(products);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Mock<ILogger<ProductCatalogClient>>().Object;
            var tokenProv = new Mock<ITokenProvider>();
            tokenProv.Setup(t => t.EnsureTokenAsync(default)).Returns(Task.CompletedTask);
            tokenProv.SetupGet(t => t.CurrentToken).Returns(string.Empty);

            var svc = new ProductCatalogClient(client, cache, logger, tokenProv.Object);

            // Act
            var (items, total) = await svc.GetPagedProductsAsync(1, 2);

            // Assert
            Assert.Equal(4, total);
            Assert.Equal(2, items.Count);
            // ordered ascending by price: D(2), B(5), C(7), A(10)
            Assert.Equal(2m, items[0].Price);
            Assert.Equal(5m, items[1].Price);
        }

        [Fact]
        public async Task GetCheapestProductsAsync_ReturnsCheapestN()
        {
            // Arrange
            var products = new List<ProductDto>
            {
                new ProductDto { Id = 1, Name = "A", Price = 10m },
                new ProductDto { Id = 2, Name = "B", Price = 5m },
                new ProductDto { Id = 3, Name = "C", Price = 7m },
                new ProductDto { Id = 4, Name = "D", Price = 2m }
            };
            var client = CreateHttpClientWithProducts(products);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Mock<ILogger<ProductCatalogClient>>().Object;
            var tokenProv = new Mock<ITokenProvider>();
            tokenProv.Setup(t => t.EnsureTokenAsync(default)).Returns(Task.CompletedTask);
            tokenProv.SetupGet(t => t.CurrentToken).Returns(string.Empty);

            var svc = new ProductCatalogClient(client, cache, logger, tokenProv.Object);

            // Act
            var cheapest = await svc.GetCheapestProductsAsync(2);

            // Assert
            Assert.Equal(2, cheapest.Count);
            Assert.Equal(2m, cheapest[0].Price);
            Assert.Equal(5m, cheapest[1].Price);
        }
    }
}
