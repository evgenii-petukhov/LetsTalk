using FluentAssertions;
using LetsTalk.Server.Configuration.Validation;
using LetsTalk.Server.Persistence.Enums;
using Microsoft.Extensions.Configuration;

namespace LetsTalk.Server.Configuration.Tests;

[TestFixture]
public class FileStorageModeTests
{
    [TestCase("Local", FileStorageTypes.Local)]
    [TestCase("AmazonS3", FileStorageTypes.AmazonS3)]
    [TestCase("AzureBlobStorage", FileStorageTypes.AzureBlobStorage)]
    [TestCase("unknown", FileStorageTypes.Local)]
    [TestCase("", FileStorageTypes.Local)]
    [TestCase(null, FileStorageTypes.Local)]
    public void Of_ShouldReadTheModeLikeFeaturesSettingsService(string? value, FileStorageTypes expected)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Features:FileStorage"] = value })
            .Build();

        // Act & Assert
        FileStorageMode.Of(configuration).Should().Be(expected);
    }
}
