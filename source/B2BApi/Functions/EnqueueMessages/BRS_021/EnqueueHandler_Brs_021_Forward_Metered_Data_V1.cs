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

using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Extensions;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;

public sealed class EnqueueHandler_Brs_021_Forward_Metered_Data_V1(
    IOutgoingMessagesClient outgoingMessagesClient,
    ILogger<EnqueueHandler_Brs_021_Forward_Metered_Data_V1> logger)
    : EnqueueActorMessagesValidatedHandlerBase<MeteredDataForMeteringPointAcceptedV1, MeteredDataForMeteringPointRejectedV1>(logger)
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly ILogger _logger = logger;

    protected override async Task EnqueueAcceptedMessagesAsync(string orchestrationInstanceId, MeteredDataForMeteringPointAcceptedV1 acceptedData)
    {
        _logger.LogInformation(
            "Received enqueue accepted message(s) for BRS 021. Data: {0}",
            acceptedData);

        // TODO: Call actual logic that enqueues accepted messages instead
        await Task.CompletedTask.ConfigureAwait(false);
    }

    protected override async Task EnqueueRejectedMessagesAsync(string orchestrationInstanceId, MeteredDataForMeteringPointRejectedV1 rejectedData)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 021. Data: {0}",
            rejectedData);

        var meteredDataForMeteringPointRejectedDto = new MeteredDataForMeteringPointRejectedDto(
            rejectedData.EventId,
            rejectedData.BusinessReason,
            rejectedData.ReceiverId,
            rejectedData.ReceiverRole,
            rejectedData.ProcessId,
            rejectedData.ExternalId,
            new AcknowledgementDto(
                rejectedData.AcknowledgementV1.ReceivedMarketDocumentCreatedDateTime,
                rejectedData.AcknowledgementV1.ReceivedMarketDocumentTransactionId,
                rejectedData.AcknowledgementV1.ReceivedMarketDocumentProcessProcessType,
                rejectedData.AcknowledgementV1.ReceivedMarketDocumentRevisionNumber,
                rejectedData.AcknowledgementV1.ReceivedMarketDocumentTitle,
                rejectedData.AcknowledgementV1.ReceivedMarketDocumentType,
                rejectedData.AcknowledgementV1.Reason.Select(r => new ReasonDto(r.Code, r.Text)).ToList(),
                rejectedData.AcknowledgementV1.InErrorPeriod.Select(
                        p => new TimePeriodDto(
                            new Interval(p.TimeInterval.Start.ToInstant(), p.TimeInterval.End.ToInstant()),
                            p.Reason.Select(r => new ReasonDto(r.Code, r.Text)).ToList()))
                    .ToList(),
                rejectedData.AcknowledgementV1.Series.Select(
                        s => new SeriesDto(s.MRID, s.Reason.Select(r => new ReasonDto(r.Code, r.Text)).ToList()))
                    .ToList(),
                rejectedData.AcknowledgementV1.OriginalMktActivityRecord.Select(
                        o => new MktActivityRecordDto(
                            o.MRID,
                            o.Reason.Select(r => new ReasonDto(r.Code, r.Text)).ToList()))
                    .ToList(),
                rejectedData.AcknowledgementV1.RejectedTimeSeries.Select(
                        t => new TimeSeriesDto(
                            t.MRID,
                            t.Version,
                            t.InErrorPeriod.Select(
                                    p => new TimePeriodDto(
                                        new Interval(p.TimeInterval.Start.ToInstant(), p.TimeInterval.End.ToInstant()),
                                        p.Reason.Select(r => new ReasonDto(r.Code, r.Text)).ToList()))
                                .ToList(),
                            t.Reason.Select(r => new ReasonDto(r.Code, r.Text)).ToList()))
                    .ToList()));

        await _outgoingMessagesClient.EnqueueAndCommitAsync(meteredDataForMeteringPointRejectedDto, CancellationToken.None).ConfigureAwait(false);
    }
}
