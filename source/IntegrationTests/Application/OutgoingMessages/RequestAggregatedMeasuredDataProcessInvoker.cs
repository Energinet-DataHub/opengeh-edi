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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Commands;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using MediatR;
using GridAreaDetails = Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.GridAreaDetails;
using Point = Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.Point;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class RequestAggregatedMeasuredDataProcessInvoker
{
    private readonly IMediator _mediator;
    private readonly ProcessContext _processContext;

    public RequestAggregatedMeasuredDataProcessInvoker(IMediator mediator, ProcessContext processContext)
    {
        _mediator = mediator;
        _processContext = processContext;
    }

    public async Task HasBeenAcceptedAsync()
    {
        var marketMessage = new RequestAggregatedMeasureDataMarketDocumentBuilder().Build();
        await _mediator.Send(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);
        var process = GetProcess(marketMessage.SenderNumber);
        process!.WasSentToWholesale();

        // ReSharper disable once MethodHasAsyncOverload -- Test Event_registration_is_omitted_if_run_in_parallel fails if this is async
        _processContext.SaveChanges();

        var acceptedAggregation = CreateAggregatedTimeSerie();
        await _mediator.Send(new AcceptedAggregatedTimeSerie(process.ProcessId.Id, new List<AggregatedTimeSerie> { acceptedAggregation })).ConfigureAwait(false);
    }

    private static AggregatedTimeSerie CreateAggregatedTimeSerie()
    {
        var points = Array.Empty<Point>();

        return new AggregatedTimeSerie(
            points,
            MeteringPointType.Consumption.Name,
            MeasurementUnit.Kwh.Name,
            Resolution.Hourly.Name,
            new GridAreaDetails("805", "1234567891045"));
    }

    private AggregatedMeasureDataProcess? GetProcess(string senderId)
    {
        return _processContext.AggregatedMeasureDataProcesses
            .ToList()
            .FirstOrDefault(x => x.RequestedByActorId.Value == senderId);
    }
}
