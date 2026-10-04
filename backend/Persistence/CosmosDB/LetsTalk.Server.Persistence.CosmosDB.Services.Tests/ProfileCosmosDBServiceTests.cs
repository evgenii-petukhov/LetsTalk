using AutoMapper;
using FluentAssertions;
using LetsTalk.Server.Persistence.AgnosticServices.Models;
using LetsTalk.Server.Persistence.CosmosDB.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.Enums;
using Moq;

namespace LetsTalk.Server.Persistence.CosmosDB.Services.Tests;

[TestFixture]
public class ProfileCosmosDBServiceTests
{
    private const string AccountId = "acc-001";
    private const string FirstName = "John";
    private const string LastName = "Doe";
    private const string ImageId = "img-001";
    private const int ImageWidth = 200;
    private const int ImageHeight = 200;
    private const ImageFormats TestImageFormat = ImageFormats.Jpeg;
    private const FileStorageTypes TestStorageType = FileStorageTypes.AmazonS3;

    private Mock<IAccountRepository> _mockAccountRepository;
    private Mock<IMapper> _mockMapper;
    private ProfileCosmosDBService _service;

    [SetUp]
    public void SetUp()
    {
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockMapper = new Mock<IMapper>();
        _service = new ProfileCosmosDBService(_mockAccountRepository.Object, _mockMapper.Object);
    }

    [TestFixture]
    public class GetByIdAsyncTests : ProfileCosmosDBServiceTests
    {
        [Test]
        public async Task GetByIdAsync_WithValidId_ShouldReturnMappedProfile()
        {
            // Arrange
            var account = new Account
            {
                Id = AccountId,
                FirstName = FirstName,
                LastName = LastName
            };

            var expectedProfile = new ProfileServiceModel
            {
                Id = AccountId,
                FirstName = FirstName,
                LastName = LastName
            };

            _mockAccountRepository
                .Setup(x => x.GetByIdAsync(AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);

            _mockMapper
                .Setup(x => x.Map<ProfileServiceModel>(account))
                .Returns(expectedProfile);

            // Act
            var result = await _service.GetByIdAsync(AccountId);

            // Assert
            result.Should().BeEquivalentTo(expectedProfile);
            _mockAccountRepository.Verify(x => x.GetByIdAsync(AccountId, It.IsAny<CancellationToken>()), Times.Once);
            _mockMapper.Verify(x => x.Map<ProfileServiceModel>(account), Times.Once);
        }

        [Test]
        public async Task GetByIdAsync_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var account = new Account { Id = AccountId };
            var expectedProfile = new ProfileServiceModel { Id = AccountId };

            _mockAccountRepository
                .Setup(x => x.GetByIdAsync(AccountId, cancellationToken))
                .ReturnsAsync(account);

            _mockMapper
                .Setup(x => x.Map<ProfileServiceModel>(account))
                .Returns(expectedProfile);

            // Act
            var result = await _service.GetByIdAsync(AccountId, cancellationToken);

            // Assert
            result.Should().BeEquivalentTo(expectedProfile);
            _mockAccountRepository.Verify(x => x.GetByIdAsync(AccountId, cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class UpdateProfileAsyncTests : ProfileCosmosDBServiceTests
    {
        [Test]
        public async Task UpdateProfileAsync_WithoutImage_ShouldUpdateAndReturnMappedProfile()
        {
            // Arrange
            var updatedAccount = new Account
            {
                Id = AccountId,
                FirstName = FirstName,
                LastName = LastName
            };

            var expectedProfile = new ProfileServiceModel
            {
                Id = AccountId,
                FirstName = FirstName,
                LastName = LastName
            };

            _mockAccountRepository
                .Setup(x => x.UpdateProfileAsync(AccountId, FirstName, LastName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedAccount);

            _mockMapper
                .Setup(x => x.Map<ProfileServiceModel>(updatedAccount))
                .Returns(expectedProfile);

            // Act
            var result = await _service.UpdateProfileAsync(AccountId, FirstName, LastName);

            // Assert
            result.Should().BeEquivalentTo(expectedProfile);
            _mockAccountRepository.Verify(x => x.UpdateProfileAsync(AccountId, FirstName, LastName, It.IsAny<CancellationToken>()), Times.Once);
            _mockMapper.Verify(x => x.Map<ProfileServiceModel>(updatedAccount), Times.Once);
        }

        [Test]
        public async Task UpdateProfileAsync_WithoutImage_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var updatedAccount = new Account { Id = AccountId };
            var expectedProfile = new ProfileServiceModel { Id = AccountId };

            _mockAccountRepository
                .Setup(x => x.UpdateProfileAsync(AccountId, FirstName, LastName, cancellationToken))
                .ReturnsAsync(updatedAccount);

            _mockMapper
                .Setup(x => x.Map<ProfileServiceModel>(updatedAccount))
                .Returns(expectedProfile);

            // Act
            await _service.UpdateProfileAsync(AccountId, FirstName, LastName, cancellationToken);

            // Assert
            _mockAccountRepository.Verify(x => x.UpdateProfileAsync(AccountId, FirstName, LastName, cancellationToken), Times.Once);
        }
    }

    [TestFixture]
    public class UpdateProfileAsyncWithImageTests : ProfileCosmosDBServiceTests
    {
        [Test]
        public async Task UpdateProfileAsync_WithImage_ShouldUpdateAndReturnMappedProfile()
        {
            // Arrange
            var updatedAccount = new Account
            {
                Id = AccountId,
                FirstName = FirstName,
                LastName = LastName,
                Image = new Image
                {
                    Id = ImageId,
                    Width = ImageWidth,
                    Height = ImageHeight,
                    ImageFormatId = (int)TestImageFormat,
                    FileStorageTypeId = (int)TestStorageType
                }
            };

            var expectedProfile = new ProfileServiceModel
            {
                Id = AccountId,
                FirstName = FirstName,
                LastName = LastName,
                Image = new ImageServiceModel
                {
                    Id = ImageId,
                    FileStorageTypeId = (int)TestStorageType
                }
            };

            _mockAccountRepository
                .Setup(x => x.UpdateProfileAsync(
                    AccountId,
                    FirstName,
                    LastName,
                    ImageId,
                    ImageWidth,
                    ImageHeight,
                    TestImageFormat,
                    TestStorageType,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedAccount);

            _mockMapper
                .Setup(x => x.Map<ProfileServiceModel>(updatedAccount))
                .Returns(expectedProfile);

            // Act
            var result = await _service.UpdateProfileAsync(
                AccountId,
                FirstName,
                LastName,
                ImageId,
                ImageWidth,
                ImageHeight,
                TestImageFormat,
                TestStorageType);

            // Assert
            result.Should().BeEquivalentTo(expectedProfile);
            _mockAccountRepository.Verify(x => x.UpdateProfileAsync(
                AccountId,
                FirstName,
                LastName,
                ImageId,
                ImageWidth,
                ImageHeight,
                TestImageFormat,
                TestStorageType,
                It.IsAny<CancellationToken>()), Times.Once);
            _mockMapper.Verify(x => x.Map<ProfileServiceModel>(updatedAccount), Times.Once);
        }

        [Test]
        public async Task UpdateProfileAsync_WithImage_WithCancellationToken_ShouldPassTokenToRepository()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var updatedAccount = new Account { Id = AccountId };
            var expectedProfile = new ProfileServiceModel { Id = AccountId };

            _mockAccountRepository
                .Setup(x => x.UpdateProfileAsync(
                    AccountId,
                    FirstName,
                    LastName,
                    ImageId,
                    ImageWidth,
                    ImageHeight,
                    TestImageFormat,
                    TestStorageType,
                    cancellationToken))
                .ReturnsAsync(updatedAccount);

            _mockMapper
                .Setup(x => x.Map<ProfileServiceModel>(updatedAccount))
                .Returns(expectedProfile);

            // Act
            await _service.UpdateProfileAsync(
                AccountId,
                FirstName,
                LastName,
                ImageId,
                ImageWidth,
                ImageHeight,
                TestImageFormat,
                TestStorageType,
                cancellationToken);

            // Assert
            _mockAccountRepository.Verify(x => x.UpdateProfileAsync(
                AccountId,
                FirstName,
                LastName,
                ImageId,
                ImageWidth,
                ImageHeight,
                TestImageFormat,
                TestStorageType,
                cancellationToken), Times.Once);
        }
    }
}
