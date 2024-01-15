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

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenAPeekIsRequestedTests : TestBase
{
    private readonly OutgoingMessageDtoBuilder _outgoingMessageDtoBuilder;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;

    public WhenAPeekIsRequestedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _outgoingMessageDtoBuilder = new OutgoingMessageDtoBuilder();
        _outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
    }

    [Fact]
    public async Task When_no_messages_are_available_return_empty_result()
    {
        var message = _outgoingMessageDtoBuilder.Build();
        await EnqueueMessage(message);

        var result = await PeekMessage(MessageCategory.None);

        Assert.Null(result.Bundle);
        Assert.True(await BundleIsRegistered());
    }

    [Fact]
    public async Task A_message_bundle_is_returned()
    {
        var message = _outgoingMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(MarketRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        var result = await PeekMessage(MessageCategory.Aggregations);

        AssertXmlMessage.Document(XDocument.Load(result.Bundle!))
            .IsDocumentType(DocumentType.NotifyAggregatedMeasureData)
            .IsBusinessReason(BusinessReason.BalanceFixing)
            .HasSerieRecordCount(1);
    }

    [Fact]
    public async Task Ensure_same_bundle_is_returned_if_not_dequeued()
    {
        var message = _outgoingMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(MarketRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        var firstPeekResult = await PeekMessage(MessageCategory.Aggregations);
        var secondPeekResult = await PeekMessage(MessageCategory.Aggregations);

        Assert.NotNull(firstPeekResult.MessageId);
        Assert.NotNull(secondPeekResult.MessageId);
        Assert.Equal(firstPeekResult.Bundle!, secondPeekResult.Bundle!);
    }

    [Fact]
    public async Task The_generated_document_is_archived()
    {
        var message = _outgoingMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(MarketRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        var result = await PeekMessage(MessageCategory.Aggregations);

        await AssertMessageIsArchived(result.MessageId);
    }

    [Fact]
    public async Task The_generated_market_document_is_added_to_database()
    {
        var message = _outgoingMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(MarketRole.EnergySupplier)
            .Build();
        await EnqueueMessage(message);

        var result = await PeekMessage(MessageCategory.Aggregations);

        result.MessageId.Should().NotBeNull();
        await AssertMarketDocumentExists(result.MessageId!.Value);
    }

    private async Task<bool> BundleIsRegistered()
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var numberOfBundles = await connection
            .ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Bundles");
        return numberOfBundles == 1;
    }

    private Task<PeekResultDto> PeekMessage(MessageCategory category)
    {
        return _outgoingMessagesClient.PeekAndCommitAsync(new PeekRequestDto(ActorNumber.Create(SampleData.NewEnergySupplierNumber), category, MarketRole.EnergySupplier, DocumentFormat.Xml), CancellationToken.None);
    }

    private async Task AssertMessageIsArchived(Guid? messageId)
    {
        var sqlStatement =
            $"SELECT COUNT(*) FROM [dbo].[ArchivedMessages] WHERE Id = '{messageId}'";
        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var found = await connection.ExecuteScalarAsync<bool>(sqlStatement);
        Assert.True(found);
    }

    private async Task AssertMarketDocumentExists(Guid marketDocumentBundleId)
    {
        var sqlStatement =
            $"SELECT COUNT(*) FROM [dbo].[MarketDocuments] WHERE BundleId = '{marketDocumentBundleId}'";
        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var found = await connection.ExecuteScalarAsync<bool>(sqlStatement);

        found.Should().BeTrue();
    }

    private async Task EnqueueMessage(OutgoingMessageDto message)
    {
        await _outgoingMessagesClient.EnqueueAsync(message);
        await GetService<ActorMessageQueueContext>().SaveChangesAsync();
    }
}
