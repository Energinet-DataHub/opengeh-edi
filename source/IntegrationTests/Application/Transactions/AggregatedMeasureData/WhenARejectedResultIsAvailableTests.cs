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
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Domain.Actors;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.RejectedRequestAggregatedMeasureData;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.Edi.Responses;
using Infrastructure.Configuration.DataAccess;
using Infrastructure.OutgoingMessages.Common;
using IntegrationTests.Assertions;
using IntegrationTests.Fixtures;
using Xunit;
using Xunit.Categories;
using RejectReason = Energinet.DataHub.Edi.Responses.RejectReason;

namespace IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class WhenARejectedResultIsAvailableTests : TestBase
{
    private readonly B2BContext _b2BContext;

    public WhenARejectedResultIsAvailableTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _b2BContext = GetService<B2BContext>();
    }

    [Fact]
    public async Task Aggregated_measure_data_response_is_rejected()
    {
        // Arrange
        var process = BuildProcess();
        var rejectReason = new RejectReason()
        {
            ErrorCode = ErrorCodes.NoDataForPeriod,
        };
        var rejectEvent = new AggregatedTimeSeriesRequestRejected()
        {
            RejectReasons = { rejectReason },
        };

        // Act
        await HavingReceivedInboxEventAsync(nameof(AggregatedTimeSeriesRequestRejected), rejectEvent, process.ProcessId.Id).ConfigureAwait(false);

        // Assert
        var outgoingMessage = await OutgoingMessageAsync(MarketRole.BalanceResponsibleParty, BusinessReason.BalanceFixing);
        outgoingMessage
            .HasBusinessReason(CimCode.To(process.BusinessReason).Name)
            .HasReceiverId(process.RequestedByActorId.Value)
            .HasReceiverRole(MarketRole.FromCode(process.RequestedByActorRoleCode).Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasAnyMessageRecordValue<RejectedTimeSerie>(timeSerie => timeSerie.RejectReason.ErrorCode, rejectReason.ErrorCode.ToString());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _b2BContext.Dispose();
    }

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync(
        MarketRole roleOfReceiver,
        BusinessReason businessReason)
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.RejectAggregatedMeasureData.Name,
            businessReason.Name,
            roleOfReceiver,
            GetService<IDatabaseConnectionFactory>()).ConfigureAwait(false);
    }

    private AggregatedMeasureDataProcess BuildProcess()
    {
        var process = new AggregatedMeasureDataProcess(
          ProcessId.New(),
          BusinessTransactionId.Create(Guid.NewGuid().ToString()),
          SampleData.Receiver,
          SampleData.ReceiverRole.Code,
          CimCode.Of(BusinessReason.BalanceFixing),
          null,
          null,
          null,
          SampleData.StartOfPeriod,
          SampleData.EndOfPeriod,
          SampleData.GridAreaCode,
          null,
          null);

        process.WasSentToWholesale();
        _b2BContext.AggregatedMeasureDataProcesses.Add(process);
        _b2BContext.SaveChanges();
        return process;
    }
}
