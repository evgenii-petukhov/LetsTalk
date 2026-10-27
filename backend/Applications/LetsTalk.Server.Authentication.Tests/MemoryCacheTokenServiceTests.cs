using FluentAssertions;
using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Exceptions;
using LetsTalk.Server.Authentication.Models;
using LetsTalk.Server.Authentication.Services.Cache.Token;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace LetsTalk.Server.Authentication.Tests;

[TestFixture]
public class MemoryCacheTokenServiceTests
{
    private const string Token = "jwt-token";
    private const string AccountId = "42";

    private Mock<IJwtStorageService> _jwtStorageServiceMock;
    private MemoryCache _memoryCache;
    private MemoryCacheTokenService _service;

    [SetUp]
    public void SetUp()
    {
        _jwtStorageServiceMock = new Mock<IJwtStorageService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _service = new MemoryCacheTokenService(_jwtStorageServiceMock.Object, _memoryCache);
    }

    [TearDown]
    public void TearDown()
    {
        _memoryCache.Dispose();
    }

    [Test]
    public async Task GetAccountIdAsync_WhenTokenNotRevoked_ShouldReturnAccountId()
    {
        // Arrange
        SetupValidToken();

        // Act
        var result = await _service.GetAccountIdAsync(Token);

        // Assert
        result.Should().Be(AccountId);
    }

    [Test]
    public async Task GetAccountIdAsync_WhenTokenRevoked_ShouldReturnNull()
    {
        // Arrange
        SetupValidToken();
        await _service.GetAccountIdAsync(Token);
        await _service.RevokeAsync(Token);

        // Act
        var result = await _service.GetAccountIdAsync(Token);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetAccountIdAsync_WhenRevokedTokenStillHasValidSignature_ShouldNotRevalidate()
    {
        // Arrange
        SetupValidToken();
        await _service.RevokeAsync(Token);
        _jwtStorageServiceMock.Invocations.Clear();

        // Act
        var result = await _service.GetAccountIdAsync(Token);

        // Assert
        result.Should().BeNull();
        _jwtStorageServiceMock.Verify(x => x.GetStoredTokenAsync(It.IsAny<string?>()), Times.Never);
    }

    [Test]
    public async Task RevokeAsync_WhenTokenInvalid_ShouldNotThrow()
    {
        // Arrange
        _jwtStorageServiceMock
            .Setup(x => x.GetStoredTokenAsync(Token))
            .ThrowsAsync(new AppException("Invalid token"));

        // Act
        var act = () => _service.RevokeAsync(Token);

        // Assert
        await act.Should().NotThrowAsync();
    }

    private void SetupValidToken()
    {
        _jwtStorageServiceMock
            .Setup(x => x.GetStoredTokenAsync(Token))
            .ReturnsAsync(new StoredToken
            {
                AccountId = AccountId,
                Token = Token,
                ValidTo = DateTime.UtcNow.AddMinutes(10)
            });
    }
}
