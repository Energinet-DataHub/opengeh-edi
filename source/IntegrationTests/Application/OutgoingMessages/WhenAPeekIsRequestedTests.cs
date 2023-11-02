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
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Application.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Domain.Documents;
using Energinet.DataHub.EDI.Process.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using MediatR;
using Xunit;
using PeekResult = Energinet.DataHub.EDI.Process.Application.OutgoingMessages.PeekResult;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenAPeekIsRequestedTests : ProcessTestBase
{
    private readonly RequestAggregatedMeasuredDataProcessInvoker _requestAggregatedMeasuredDataProcessInvoker;

    public WhenAPeekIsRequestedTests(ProcessDatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _requestAggregatedMeasuredDataProcessInvoker =
            new RequestAggregatedMeasuredDataProcessInvoker(GetService<IMediator>(), GetService<ProcessContext>());
    }

    [Fact]
    public async Task When_no_messages_are_available_return_empty_result()
    {
        await _requestAggregatedMeasuredDataProcessInvoker.HasBeenAcceptedAsync();

        var result = await PeekMessage(MessageCategory.None);

        Assert.Null(result.Bundle);
        Assert.True(await BundleIsRegistered());
    }

    [Fact]
    public async Task A_message_bundle_is_returned()
    {
        await _requestAggregatedMeasuredDataProcessInvoker.HasBeenAcceptedAsync();

        var result = await PeekMessage(MessageCategory.Aggregations);

        Assert.NotNull(result.Bundle);

        AssertXmlMessage.Document(XDocument.Load(result.Bundle!))
            .IsDocumentType(DocumentType.NotifyAggregatedMeasureData)
            .IsBusinessReason(BusinessReason.PreliminaryAggregation)
            .HasSerieRecordCount(1);
    }

    [Fact]
    public async Task Ensure_same_bundle_is_returned_if_not_dequeued()
    {
        await _requestAggregatedMeasuredDataProcessInvoker.HasBeenAcceptedAsync();

        var firstPeekResult = await PeekMessage(MessageCategory.Aggregations);
        var secondPeekResult = await PeekMessage(MessageCategory.Aggregations);

        Assert.NotNull(firstPeekResult.MessageId);
        Assert.NotNull(secondPeekResult.MessageId);
        Assert.Equal(firstPeekResult.Bundle!, secondPeekResult.Bundle!);
    }

    [Fact]
    public async Task The_generated_document_is_archived()
    {
        await _requestAggregatedMeasuredDataProcessInvoker.HasBeenAcceptedAsync();

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
