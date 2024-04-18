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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.Edi.Responses;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;
using RejectReason = Energinet.DataHub.Edi.Responses.RejectReason;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class WhenARejectedResultIsAvailableTests : TestBase
{
    private readonly ProcessContext _processContext;

    public WhenARejectedResultIsAvailableTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _processContext = GetService<ProcessContext>();
    }

    [Fact]
    public async Task Aggregated_measure_data_response_is_rejected()
    {
        // Arrange
        var expectedEventId = "expected-event-id";
        var process = await BuildProcess();
        var rejectReason = new RejectReason()
        {
            ErrorCode = "ER0",
        };
        var rejectReason2 = new RejectReason()
        {
            ErrorCode = "ER1",
        };
        var rejectEvent = new AggregatedTimeSeriesRequestRejected()
        {
            RejectReasons = { rejectReason, rejectReason2 },
        };

        // Act
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestRejected), rejectEvent, process.ProcessId.Id, expectedEventId);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(ActorRole.BalanceResponsibleParty, BusinessReason.BalanceFixing);
        outgoingMessage
            .HasEventId(expectedEventId)
            .HasProcessId(process.ProcessId)
            .HasBusinessReason(process.BusinessReason)
            .HasReceiverId(process.RequestedByActorId.Value)
            .HasReceiverRole(process.RequestedByActorRoleCode)
            .HasRelationTo(process.InitiatedByMessageId)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasMessageRecordValue<RejectedEnergyResultMessageSerie>(timeSerie => timeSerie.RejectReasons.First().ErrorCode, rejectReason.ErrorCode)
            .HasMessageRecordValue<RejectedEnergyResultMessageSerie>(timeSerie => timeSerie.RejectReasons.Last().ErrorCode, rejectReason2.ErrorCode)
            .HasMessageRecordValue<RejectedEnergyResultMessageSerie>(timeSerie => timeSerie.OriginalTransactionIdReference, process.BusinessTransactionId.Id);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _processContext.Dispose();
    }

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync(
        ActorRole roleOfReceiver,
        BusinessReason businessReason)
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.RejectRequestAggregatedMeasureData.Name,
            businessReason.Name,
            roleOfReceiver,
            GetService<IDatabaseConnectionFactory>(),
            GetService<IFileStorageClient>());
    }

    private async Task<AggregatedMeasureDataProcess> BuildProcess()
    {
        var process = new AggregatedMeasureDataProcess(
          ProcessId.New(),
          BusinessTransactionId.Create(Guid.NewGuid().ToString()),
          SampleData.ReceiverNumber,
          SampleData.BalanceResponsibleParty.Code,
          BusinessReason.BalanceFixing,
          MessageId.New(),
          null,
          null,
          SampleData.StartOfPeriod,
          SampleData.EndOfPeriod,
          SampleData.GridAreaCode,
          null,
          null,
          null);

        process.SendToWholesale();
        _processContext.AggregatedMeasureDataProcesses.Add(process);
        await _processContext.SaveChangesAsync();
        return process;
    }
}
