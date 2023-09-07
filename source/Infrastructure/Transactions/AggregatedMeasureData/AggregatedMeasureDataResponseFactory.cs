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
        return process.MeteringPointType switch
        {
            "E18" => TimeSeriesType.Production,
            "E20" => TimeSeriesType.NetExchangePerGa,
            "E17" => process.SettlementMethod switch
            {
                "D01" => TimeSeriesType.NonProfiledConsumption,
                "E02" => TimeSeriesType.FlexConsumption,
                null => TimeSeriesType.TotalConsumption,
                _ => throw new InvalidOperationException(
                    $"Unknown time series type for metering point type {process.MeteringPointType} and settlement method {process.SettlementMethod}"),
            },
            _ => throw new InvalidOperationException(
                $"Unknown time series type for metering point type {process.MeteringPointType} and settlement method {process.SettlementMethod}"),
        };
    }
}
