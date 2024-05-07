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
using Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Domain;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.Edi.Responses;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;
using ChargeType = Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices.ChargeType;
using RejectReason = Energinet.DataHub.Edi.Responses.RejectReason;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.WholesaleServices;

[IntegrationTest]
public sealed class WhenARejectedWholesaleServicesIsAvailableTests : TestBase
{
    private readonly ProcessContext _processContext;

    public WhenARejectedWholesaleServicesIsAvailableTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _processContext = GetService<ProcessContext>();
    }

    [Fact]
    public async Task Wholesale_services_response_is_rejected()
    {
        // Arrange
        var expectedEventId = Guid.NewGuid().ToString();
        var process = await BuildProcess();
        var rejectReason = new RejectReason { ErrorCode = "ER0" };
        var rejectReason2 = new RejectReason { ErrorCode = "ER1" };
        var rejectEvent = new WholesaleServicesRequestRejected { RejectReasons = { rejectReason, rejectReason2 } };

        // Act
        await HavingReceivedInboxEventAsync(
            nameof(WholesaleServicesRequestRejected),
            rejectEvent,
            process.ProcessId.Id,
            expectedEventId);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(
            ActorRole.EnergySupplier,
            BusinessReason.WholesaleFixing);
        outgoingMessage
            .HasProcessId(process.ProcessId)
            .HasEventId(expectedEventId)
            .HasBusinessReason(process.BusinessReason)
            .HasReceiverId(process.RequestedByActor.ActorNumber.Value)
            .HasReceiverRole(process.RequestedByActor.ActorRole.Code)
            .HasRelationTo(process.InitiatedByMessageId)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasProcessType(ProcessType.RequestWholesaleResults)
            .HasMessageRecordValue<RejectedWholesaleServicesMessageSeries>(
                messageSeries => messageSeries.RejectReasons.First().ErrorCode,
                rejectReason.ErrorCode)
            .HasMessageRecordValue<RejectedWholesaleServicesMessageSeries>(
                messageSeries => messageSeries.RejectReasons.Last().ErrorCode,
                rejectReason2.ErrorCode)
            .HasMessageRecordValue<RejectedWholesaleServicesMessageSeries>(
                messageSeries => messageSeries.OriginalTransactionIdReference,
                process.BusinessTransactionId.Value);
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
            DocumentType.RejectRequestWholesaleSettlement.Name,
            businessReason.Name,
            roleOfReceiver,
            GetService<IDatabaseConnectionFactory>(),
            GetService<IFileStorageClient>());
    }

    private async Task<WholesaleServicesProcess> BuildProcess()
    {
        var requestedByActor = RequestedByActor.From(ActorNumber.Create("8200000007743"), ActorRole.EnergySupplier);

        var process = new WholesaleServicesProcess(
            ProcessId.New(),
            requestedByActor,
            OriginalActor.From(requestedByActor),
            TransactionId.New(),
            MessageId.New(),
            BusinessReason.WholesaleFixing,
            SampleData.StartOfPeriod,
            SampleData.EndOfPeriod,
            SampleData.GridAreaCode,
            "8200000007743",
            null,
            null,
            null,
            new ChargeType[] { new(ChargeTypeId.New(), "1", "ST1") },
            new[] { SampleData.GridAreaCode });

        process.SendToWholesale();
        _processContext.WholesaleServicesProcesses.Add(process);
        await _processContext.SaveChangesAsync();
        return process;
    }
}
