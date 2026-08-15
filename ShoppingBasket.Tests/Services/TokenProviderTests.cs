using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingBasket.API.Services;
using System.Net;
using System.Text.Json;
using Xunit;

namespace ShoppingBasket.Tests.Services
{
    internal class TokenProviderFakeHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public TokenProviderFakeHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    public class TokenProviderTests
    {
        [Fact]
        public async Task GetTokenAsync_ReturnsNull_WhenLoginEmailNotConfigured()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.SetupGet(c => c["CodeChallengeApi:LoginEmail"]).Returns((string?)null);
            var client = new HttpClient(new TokenProviderFakeHandler(new HttpResponseMessage(HttpStatusCode.OK)));
            var logger = new Mock<ILogger<TokenProvider>>().Object;
            var tp = new TokenProvider(client, configMock.Object, logger);

            // Act
            var (token, expiry) = await tp.GetTokenAsync();

            // Assert
            Assert.Null(token);
            Assert.Null(expiry);
        }

        [Fact]
        public async Task GetTokenAsync_ParsesTokenAndExpiry()
        {
            // Arrange
            var payload = JsonSerializer.Serialize(new { token = "abc123", expiresIn = 3600 });
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(payload) };
            var client = new HttpClient(new TokenProviderFakeHandler(response)) { BaseAddress = new Uri("http://localhost/") };
            var configMock = new Mock<IConfiguration>();
            configMock.SetupGet(c => c["CodeChallengeApi:LoginEmail"]).Returns("me@example.com");
            var logger = new Mock<ILogger<TokenProvider>>().Object;
            var tp = new TokenProvider(client, configMock.Object, logger);

            // Act
            var (token, expiry) = await tp.GetTokenAsync();

            // Assert
            Assert.Equal("abc123", token);
            Assert.NotNull(expiry);
            Assert.True(expiry.Value > DateTimeOffset.UtcNow);
        }

        [Fact]
        public async Task EnsureTokenAsync_SetsCurrentToken()
        {
            // Arrange
            var payload = JsonSerializer.Serialize(new { token = "abc123", expiresIn = 3600 });
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(payload) };
            var client = new HttpClient(new TokenProviderFakeHandler(response)) { BaseAddress = new Uri("http://localhost/") };
            var configMock = new Mock<IConfiguration>();
            configMock.SetupGet(c => c["CodeChallengeApi:LoginEmail"]).Returns("me@example.com");
            var logger = new Mock<ILogger<TokenProvider>>().Object;
            var tp = new TokenProvider(client, configMock.Object, logger);

            // Act
            await tp.EnsureTokenAsync();

            // Assert
            Assert.Equal("abc123", tp.CurrentToken);
        }

        [Fact]
        public async Task GetTokenAsync_ReturnsNull_OnNonSuccessStatus()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var client = new HttpClient(new TokenProviderFakeHandler(response)) { BaseAddress = new Uri("http://localhost/") };
            var configMock = new Mock<IConfiguration>();
            configMock.SetupGet(c => c["CodeChallengeApi:LoginEmail"]).Returns("me@example.com");
            var logger = new Mock<ILogger<TokenProvider>>().Object;
            var tp = new TokenProvider(client, configMock.Object, logger);

            // Act
            var (token, expiry) = await tp.GetTokenAsync();

            // Assert
            Assert.Null(token);
            Assert.Null(expiry);
        }

        [Fact]
        public async Task GetTokenAsync_ReturnsNull_OnEmptyContent()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
            var client = new HttpClient(new TokenProviderFakeHandler(response));
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["CodeChallengeApi:LoginEmail"]).Returns("me@example.com");
            var logger = new Mock<ILogger<TokenProvider>>().Object;
            var tp = new TokenProvider(client, configMock.Object, logger);

            // Act
            var (token, expiry) = await tp.GetTokenAsync();

            // Assert
            Assert.Null(token);
            Assert.Null(expiry);
        }

        [Fact]
        public async Task EnsureTokenAsync_DoesNotCallHttp_WhenTokenIsStillValid()
        {
            // Arrange
            var payload = JsonSerializer.Serialize(new { token = "abc123", expiresIn = 3600 });
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(payload) };
            var handler = new CountingHandler(response);
            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["CodeChallengeApi:LoginEmail"]).Returns("me@example.com");
            var logger = new Mock<ILogger<TokenProvider>>().Object;
            var tp = new TokenProvider(client, configMock.Object, logger);

            // Act
            await tp.EnsureTokenAsync();
            await tp.EnsureTokenAsync();

            // Assert - handler should have been called only once
            Assert.Equal(1, handler.CallCount);
            Assert.Equal("abc123", tp.CurrentToken);
        }
    }
}
