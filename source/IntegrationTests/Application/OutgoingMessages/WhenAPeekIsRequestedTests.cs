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
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Common.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Contracts;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using MediatR;
using Xunit;
using DocumentType = Energinet.DataHub.EDI.Common.DocumentType;
using MessageCategory = Energinet.DataHub.EDI.Common.MessageCategory;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenAPeekIsRequestedTests : TestBase
{
    private readonly IEnqueueMessage _enqueueMessage;
    private readonly OutgoingMessageDtoBuilder _outgoingMessageDtoBuilder;

    public WhenAPeekIsRequestedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _outgoingMessageDtoBuilder = new OutgoingMessageDtoBuilder();
        _enqueueMessage = GetService<IEnqueueMessage>();
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

    private static string ConvertMemoryStreamToString(Stream memoryStream)
    {
        // Reset the position of the MemoryStream to the beginning
        memoryStream.Seek(0, SeekOrigin.Begin);

        // Create a StreamReader and read the contents of the MemoryStream
        using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
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
            $"SELECT COUNT(*) FROM [dbo].[ArchivedMessages] WHERE Id = '{messageId}'";
        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var found = await connection.ExecuteScalarAsync<bool>(sqlStatement);
        Assert.True(found);
    }

    private async Task EnqueueMessage(OutgoingMessageDto message)
    {
        await _enqueueMessage.EnqueueAsync(message);
        await GetService<ActorMessageQueueContext>().SaveChangesAsync();
    }
}
