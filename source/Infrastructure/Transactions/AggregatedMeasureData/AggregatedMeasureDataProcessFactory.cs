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
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.WellKnownTypes;
using Serie = Energinet.DataHub.Edi.Responses.Serie;

namespace Infrastructure.Transactions.AggregatedMeasureData;

public static class AggregatedMeasureDataProcessFactory
{
    // TODO: consider moving this to another class
    public static ServiceBusMessage CreateServiceBusMessage(AggregatedMeasureDataProcess process)
    {
        var bodyFromWholesaleMock = CreateResponseFromWholeSaleTemp(process);
        var message = new ServiceBusMessage()
        {
            Body = new BinaryData(bodyFromWholesaleMock),
            Subject = nameof(AggregatedTimeSeriesRequestAccepted),
            MessageId = process.ProcessId.Id.ToString(),
        };
        message.ApplicationProperties.Add("ReferenceId", process.ProcessId.Id.ToString());

        return message;
    }

    private static AggregatedTimeSeriesRequestAccepted CreateResponseFromWholeSaleTemp(
        AggregatedMeasureDataProcess aggregatedMeasureDataProcess)
    {
        ArgumentNullException.ThrowIfNull(aggregatedMeasureDataProcess);

        var wholesaleResponse = new AggregatedTimeSeriesRequestAccepted();
        wholesaleResponse.Series.Add(CreateSerie(aggregatedMeasureDataProcess));

        return wholesaleResponse;
    }

    private static Serie CreateSerie(AggregatedMeasureDataProcess aggregatedMeasureDataProcess)
    {
        var quantity = new DecimalValue() { Units = 12345, Nanos = 123450000, };
        var point = new TimeSeriesPoint()
        {
            Quantity = quantity,
            QuantityQuality = QuantityQuality.Incomplete,
        };

        var period = new Period()
        {
            StartOfPeriod = new Timestamp() { Seconds = aggregatedMeasureDataProcess.StartOfPeriod.ToUnixTimeSeconds(), },
            EndOfPeriod = new Timestamp() { Seconds = aggregatedMeasureDataProcess.EndOfPeriod?.ToUnixTimeSeconds() ?? 1, },
            Resolution = Resolution.Pt15M,
        };

        return new Serie()
        {
#pragma warning disable CA1305
            SettlementVersion = aggregatedMeasureDataProcess.SettlementVersion ?? "0",
#pragma warning restore CA1305
            GridArea = aggregatedMeasureDataProcess.MeteringGridAreaDomainId,
            Product = Product.Tarif,
            QuantityUnit = QuantityUnit.Kwh,
            Period = period,
            TimeSeriesPoints = { point },
        };
    }
}
