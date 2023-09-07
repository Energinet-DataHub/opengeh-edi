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
using Azure.Messaging.ServiceBus;
using Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.Edi.Requests;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Infrastructure.Transactions.AggregatedMeasureData;

public static class AggregatedMeasureDataResponseFactory
{
    public static ServiceBusMessage CreateServiceBusMessage(AggregatedMeasureDataProcess process)
    {
        if (process == null) throw new ArgumentNullException(nameof(process));

        var body = CreateAggregatedMeasureDataRequest(process);

        var message = new ServiceBusMessage()
        {
            Body = new BinaryData(body.ToByteArray()),
            Subject = body.GetType().Name,
            MessageId = process.ProcessId.Id.ToString(),
        };

        message.ApplicationProperties.Add("ReferenceId", process.ProcessId.Id.ToString());
        return message;
    }

    private static IMessage CreateAggregatedMeasureDataRequest(AggregatedMeasureDataProcess process)
    {
        var response = new AggregatedTimeSeriesRequest()
        {
            Period = MapPeriod(process),
            AggregationPerGridarea = MapGridArea(process),
            TimeSeriesType = MapTimeSeriesType(process),
        };
        return response;
    }

    private static Energinet.DataHub.Edi.Requests.Period MapPeriod(AggregatedMeasureDataProcess process)
    {
        return new Energinet.DataHub.Edi.Requests.Period()
        {
            StartOfPeriod = new Timestamp() { Seconds = process.StartOfPeriod.ToUnixTimeSeconds(), },
            EndOfPeriod = new Timestamp() { Seconds = process.EndOfPeriod?.ToUnixTimeSeconds() ?? 1, },
        };
    }

    private static AggregationPerGridArea MapGridArea(AggregatedMeasureDataProcess process)
    {
        return new AggregationPerGridArea()
        {
            GridAreaCode = process.MeteringGridAreaDomainId,
            GridResponsibleId = process.RequestedByActorId.Value,
        };
    }

    private static TimeSeriesType MapTimeSeriesType(AggregatedMeasureDataProcess process)
    {
        return process.SettlementMethod switch
        {
            _ => TimeSeriesType.Production,
            // These might depend on more than one parameter, see documentation below.
            // https://energinet.atlassian.net/wiki/spaces/D3/pages/275677228/EDI+domain
            /*"E18" => TimeSeriesType.Production,
            "E02" => TimeSeriesType.NonProfiledConsumption,
            "D01" => TimeSeriesType.FlexConsumption,
            "4" => TimeSeriesType.NetExchangePerGa,
            "5" => TimeSeriesType.NetExchangePerNeighboringGa,
            "6" => TimeSeriesType.GridLoss,
            "7" => TimeSeriesType.NegativeGridLoss,
            "8" => TimeSeriesType.PositiveGridLoss,
            "9" => TimeSeriesType.TotalConsumption,
            _ => null,*/
        };
    }
}
