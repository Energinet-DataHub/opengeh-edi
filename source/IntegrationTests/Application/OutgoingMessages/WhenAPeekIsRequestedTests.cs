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
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dapper;
using Energinet.DataHub.EDI.ActorMessageQueue.Application.OutgoingMessages;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using MediatR;
using Xunit;
using DocumentType = Energinet.DataHub.EDI.Common.DocumentType;
using MessageCategory = Energinet.DataHub.EDI.Common.MessageCategory;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenAPeekIsRequestedTests : ActorMessageQueueTestBase
{
    private readonly EnqueueMessageEventBuilder _enqueueMessageEventBuilder;

    public WhenAPeekIsRequestedTests(ActorMessageQueueDatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _enqueueMessageEventBuilder = new EnqueueMessageEventBuilder();
    }

    [Fact]
    public async Task When_no_messages_are_available_return_empty_result()
    {
        var command = _enqueueMessageEventBuilder.Build();
        await InvokeDomainEventAsync(command);

        var result = await PeekMessage(MessageCategory.None);

        Assert.Null(result.Bundle);
        Assert.True(await BundleIsRegistered());
    }

    [Fact]
    public async Task A_message_bundle_is_returned()
    {
        var command = _enqueueMessageEventBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(MarketRole.EnergySupplier)
            .Build();
        await InvokeDomainEventAsync(command);

        var result = await PeekMessage(MessageCategory.Aggregations);

        Assert.NotNull(result.Bundle);

        AssertXmlMessage.Document(XDocument.Load(result.Bundle!))
            .IsDocumentType(DocumentType.NotifyAggregatedMeasureData)
            .IsBusinessReason(BusinessReason.BalanceFixing)
            .HasSerieRecordCount(1);
    }

    [Fact]
    public async Task Ensure_same_bundle_is_returned_if_not_dequeued()
    {
        var command = _enqueueMessageEventBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(MarketRole.EnergySupplier)
            .Build();
        await InvokeDomainEventAsync(command);

        var firstPeekResult = await PeekMessage(MessageCategory.Aggregations);
        var secondPeekResult = await PeekMessage(MessageCategory.Aggregations);

        Assert.NotNull(firstPeekResult.MessageId);
        Assert.NotNull(secondPeekResult.MessageId);
        Assert.Equal(firstPeekResult.Bundle!, secondPeekResult.Bundle!);
    }

    [Fact]
    public async Task The_generated_document_is_archived()
    {
        var command = _enqueueMessageEventBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(MarketRole.EnergySupplier)
            .Build();
        await InvokeDomainEventAsync(command);

        var result = await PeekMessage(MessageCategory.Aggregations);

        await AssertMessageIsArchived(result.MessageId);
    }

    private async Task<bool> BundleIsRegistered()
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var numberOfBundles = await connection
            .ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Bundles");
        return numberOfBundles == 1;
    }

    private Task<PeekResult> PeekMessage(MessageCategory category)
    {
        var mediatr = GetService<IMediator>();
        return mediatr.Send(new PeekCommand(ActorNumber.Create(SampleData.NewEnergySupplierNumber), category, MarketRole.EnergySupplier, DocumentFormat.Xml));
    }

    private async Task AssertMessageIsArchived(Guid? messageId)
    {
        var sqlStatement =
            $"SELECT COUNT(*) FROM [dbo].[ArchivedMessages] WHERE MessageId = '{messageId}'";
        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var found = await connection.ExecuteScalarAsync<bool>(sqlStatement);
        Assert.True(found);
    }
}
