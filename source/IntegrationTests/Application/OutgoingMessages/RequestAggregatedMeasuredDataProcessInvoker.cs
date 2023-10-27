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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.Transactions.AggregatedMeasureData.Commands;
using Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages;
using MediatR;
using NodaTime.Text;
using GridAreaDetails = Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData.GridAreaDetails;
using Period = Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData.Period;
using Point = Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData.Point;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class RequestAggregatedMeasuredDataProcessInvoker
{
    private readonly IMediator _mediator;
    private readonly B2BContext _b2BContext;

    public RequestAggregatedMeasuredDataProcessInvoker(IMediator mediator, B2BContext b2BContext)
    {
        _mediator = mediator;
        _b2BContext = b2BContext;
    }

    public async Task HasBeenAcceptedAsync()
    {
        var marketMessage = new RequestAggregatedMeasureDataMarketDocumentBuilder().Build();
        await _mediator.Send(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);
        var process = GetProcess(marketMessage.SenderNumber);
        process!.IsSendingToWholesale();
        process!.WasSentToWholesale();

        // ReSharper disable once MethodHasAsyncOverload -- Test Event_registration_is_omitted_if_run_in_parallel fails if this is async
        _b2BContext.SaveChanges();

        var acceptedAggregation = CreateAggregatedTimeSerie();
        await _mediator.Send(new ResponseMessageAggregatedTimeSerie(process.ProcessId.Id, acceptedAggregation)).ConfigureAwait(false);
        await _mediator.Send(new ReceiptAggregatedTimeSeries(
            process.ProcessId.Id,
            new ReadOnlyCollection<string>(new[] { acceptedAggregation.GridAreaDetails.GridAreaCode }))).ConfigureAwait(false);
    }

    private static AggregatedTimeSerie CreateAggregatedTimeSerie()
    {
        var points = Array.Empty<Point>();

        return new AggregatedTimeSerie(
            points,
            MeteringPointType.Consumption.Name,
            MeasurementUnit.Kwh.Name,
            Resolution.Hourly.Name,
            new Period(InstantPattern.General.Parse("2022-06-17T22:00:00Z").Value, InstantPattern.General.Parse("2022-07-22T22:00:00Z").Value),
            new GridAreaDetails("805", "1234567891045"),
            SettlementVersion.FirstCorrection.Name);
    }

    private AggregatedMeasureDataProcess? GetProcess(string senderId)
    {
        return _b2BContext.AggregatedMeasureDataProcesses
            .ToList()
            .FirstOrDefault(x => x.RequestedByActorId.Value == senderId);
    }
}
