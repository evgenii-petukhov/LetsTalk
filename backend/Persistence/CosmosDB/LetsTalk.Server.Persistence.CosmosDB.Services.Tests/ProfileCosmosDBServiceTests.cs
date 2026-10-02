using AutoMapper;
using FluentAssertions;
using LetsTalk.Server.Persistence.AgnosticServices.Models;
using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.Enums;
using LetsTalk.Server.Persistence.CosmosDB.Services;
using Moq;

namespace LetsTalk.Server.Persistence.CosmosDB.Services.Tests;

[TestFixture]
public class ProfileCosmosDBServiceTests
{
    private Mock<IAccountRepository> _accountRepository = null!;
    private Mock<IMapper> _mapper = null!;
    private ProfileCosmosDBService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _accountRepository = new Mock<IAccountRepository>();
        _mapper = new Mock<IMapper>();
        _service = new ProfileCosmosDBService(_accountRepository.Object, _mapper.Object);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnMappedProfileAndForwardCancellationToken()
    {
        const string accountId = "account-1";
        var cancellationToken = new CancellationToken(canceled: true);
        var account = new Account { Id = accountId };
        var expected = new ProfileServiceModel { Id = accountId };
        _accountRepository.Setup(x => x.GetByIdAsync(accountId, cancellationToken)).ReturnsAsync(account);
        _mapper.Setup(x => x.Map<ProfileServiceModel>(account)).Returns(expected);

        var result = await _service.GetByIdAsync(accountId, cancellationToken);

        result.Should().BeSameAs(expected);
        _accountRepository.Verify(x => x.GetByIdAsync(accountId, cancellationToken), Times.Once);
        _mapper.Verify(x => x.Map<ProfileServiceModel>(account), Times.Once);
    }

    [Test]
    public async Task UpdateProfileAsync_WithoutImage_ShouldUpdateAndMapProfile()
    {
        const string accountId = "account-1";
        const string firstName = "Ada";
        const string lastName = "Lovelace";
        var cancellationToken = new CancellationToken(canceled: true);
        var account = new Account { Id = accountId, FirstName = firstName, LastName = lastName };
        var expected = new ProfileServiceModel { Id = accountId, FirstName = firstName, LastName = lastName };
        _accountRepository.Setup(x => x.UpdateProfileAsync(accountId, firstName, lastName, cancellationToken)).ReturnsAsync(account);
        _mapper.Setup(x => x.Map<ProfileServiceModel>(account)).Returns(expected);

        var result = await _service.UpdateProfileAsync(accountId, firstName, lastName, cancellationToken);

        result.Should().BeSameAs(expected);
        _accountRepository.Verify(x => x.UpdateProfileAsync(accountId, firstName, lastName, cancellationToken), Times.Once);
        _mapper.Verify(x => x.Map<ProfileServiceModel>(account), Times.Once);
    }

    [Test]
    public async Task UpdateProfileAsync_WithImage_ShouldForwardImageDetailsAndMapProfile()
    {
        const string accountId = "account-1";
        const string firstName = "Ada";
        const string lastName = "Lovelace";
        const string imageId = "image-1";
        const int width = 640;
        const int height = 480;
        const ImageFormats imageFormat = ImageFormats.Jpeg;
        const FileStorageTypes fileStorageType = FileStorageTypes.AmazonS3;
        var cancellationToken = new CancellationToken(canceled: true);
        var account = new Account { Id = accountId, FirstName = firstName, LastName = lastName };
        var expected = new ProfileServiceModel { Id = accountId };
        _accountRepository
            .Setup(x => x.UpdateProfileAsync(accountId, firstName, lastName, imageId, width, height, imageFormat, fileStorageType, cancellationToken))
            .ReturnsAsync(account);
        _mapper.Setup(x => x.Map<ProfileServiceModel>(account)).Returns(expected);

        var result = await _service.UpdateProfileAsync(
            accountId,
            firstName,
            lastName,
            imageId,
            width,
            height,
            imageFormat,
            fileStorageType,
            cancellationToken);

        result.Should().BeSameAs(expected);
        _accountRepository.Verify(
            x => x.UpdateProfileAsync(accountId, firstName, lastName, imageId, width, height, imageFormat, fileStorageType, cancellationToken),
            Times.Once);
        _mapper.Verify(x => x.Map<ProfileServiceModel>(account), Times.Once);
    }
}
