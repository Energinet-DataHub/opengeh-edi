// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Text;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.BuildingBlocks.FileStorageClient;

public class DataLakeFileStorageClientTests
{
    private readonly DataLakeFileStorageClient _sut;
    private readonly Mock<BlobContainerClient> _blobContainerClientMock;
    private readonly Mock<BlobContainerClient> _blobContainerClientObsoletedMock;
    private readonly ClockStub _clockStub;
    private readonly Instant _cutOffDate = InstantPattern.General.Parse("2025-01-01T00:00:00Z").Value;
    private readonly Mock<BlobClient> _blobClientMock;

    public DataLakeFileStorageClientTests()
    {
        Mock<IAzureClientFactory<BlobServiceClient>> clientFactoryMock = new();
        Mock<BlobServiceClient> blobServiceClientMock = new();
        _blobContainerClientMock = new();
        _blobClientMock = new();

        var options = Options.Create(
            new BlobServiceClientConnectionOptions
            {
                ClientName = "ClientName",
                ClientNameObsoleted = "ClientNameObsoleted",
                CutOffDate = _cutOffDate.ToString(),
            });

        clientFactoryMock
            .Setup(x => x.CreateClient(options.Value.ClientName))
            .Returns(blobServiceClientMock.Object);

        blobServiceClientMock
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_blobContainerClientMock.Object);

        _blobContainerClientMock
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_blobClientMock.Object);

        // Obsoleted client
        Mock<BlobServiceClient> blobServiceClientObsoletedMock = new();
        _blobContainerClientObsoletedMock = new();
        clientFactoryMock
            .Setup(x => x.CreateClient(options.Value.ClientNameObsoleted))
            .Returns(blobServiceClientObsoletedMock.Object);

        blobServiceClientObsoletedMock
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_blobContainerClientObsoletedMock.Object);

        _blobContainerClientObsoletedMock
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_blobClientMock.Object);
        // End: Obsoleted client

        _clockStub = new ClockStub();
        _sut = new DataLakeFileStorageClient(clientFactoryMock.Object, options, _clockStub);
    }

    [Fact]
    public async Task Given_Stream_When_UploadAsync_Then_ShouldCallUploadBlobAsync()
    {
        // Arrange
        var reference = new FileStorageReference(FileStorageCategory.ArchivedMessage(), "path");
        var content = "test content";
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        // Act
        await _sut.UploadAsync(reference, stream);

        // Assert
        _blobContainerClientMock.Verify(
            x => x.UploadBlobAsync(reference.Path, stream, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Given_Stream_When_UploadAsyncBeforeCutOffDate_Then_ShouldCallUploadBlobAsyncOnBlobContainerClientObsoleted()
    {
        // Arrange
        var now = _cutOffDate.Minus(Duration.FromDays(1));
        _clockStub.SetCurrentInstant(now);

        var reference = new FileStorageReference(FileStorageCategory.ArchivedMessage(), "path");
        var content = "test content";
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        // Act
        await _sut.UploadAsync(reference, stream);

        // Assert
        _blobContainerClientObsoletedMock.Verify(
            x => x.UploadBlobAsync(reference.Path, stream, It.IsAny<CancellationToken>()),
            Times.Once);
        _blobContainerClientMock.Verify(
            x => x.UploadBlobAsync(reference.Path, stream, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Given_Stream_When_UploadAsyncAfterCutOffDate_Then_ShouldCallUploadBlobAsyncOnBlobContainerClient()
    {
        // Arrange
        var now = _cutOffDate.Plus(Duration.FromDays(1));
        _clockStub.SetCurrentInstant(now);

        var reference = new FileStorageReference(FileStorageCategory.ArchivedMessage(), "path");
        var content = "test content";
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;

        // Act
        await _sut.UploadAsync(reference, stream);

        // Assert
        _blobContainerClientMock.Verify(
            x => x.UploadBlobAsync(reference.Path, stream, It.IsAny<CancellationToken>()),
            Times.Once);
        _blobContainerClientObsoletedMock.Verify(
            x => x.UploadBlobAsync(reference.Path, stream, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Given_ReferenceToUploadedBlob_When_DownloadAsync_Then_ShouldCallGetBlobClientOnBlobContainerClientObsoleted()
    {
        // Arrange
        var now = _cutOffDate.Minus(Duration.FromDays(1));
        _clockStub.SetCurrentInstant(now);

        var reference = new FileStorageReference(FileStorageCategory.ArchivedMessage(), "path");
        var content = "test content";
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;
        await _sut.UploadAsync(reference, stream);

        // Act
        var file = await _sut.DownloadAsync(reference);

        // Assert
        Assert.NotNull(file);
        _blobContainerClientObsoletedMock.Verify(
            x => x.GetBlobClient(reference.Path),
            Times.Once);
    }

    [Fact]
    public async Task Given_ReferenceToUploadedBlob_When_DownloadAsync_Then_ShouldCallGetBlobClientOnBlobContainerClient()
    {
        // Arrange
        var now = _cutOffDate.Plus(Duration.FromDays(1));
        _clockStub.SetCurrentInstant(now);

        var reference = new FileStorageReference(FileStorageCategory.ArchivedMessage(), "path");
        var content = "test content";
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        stream.Position = 0;
        await _sut.UploadAsync(reference, stream);

        var responseMock = new Mock<Response>();
        responseMock.SetupGet(r => r.Status).Returns(200);
        responseMock.SetupGet(r => r.IsError).Returns(false);
        _blobClientMock
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, responseMock.Object));

        // Act
        var file = await _sut.DownloadAsync(reference);

        // Assert
        Assert.NotNull(file);
        _blobContainerClientMock.Verify(
            x => x.GetBlobClient(reference.Path),
            Times.Once);
        _blobContainerClientObsoletedMock.Verify(
            x => x.GetBlobClient(reference.Path),
            Times.Never);
    }
}
