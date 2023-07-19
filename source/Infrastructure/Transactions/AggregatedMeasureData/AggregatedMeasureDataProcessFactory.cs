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
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.WholeSale;
using Serie = Energinet.DataHub.Edi.Responses.Serie;

namespace Infrastructure.Transactions.AggregatedMeasureData;

public static class AggregatedMeasureDataProcessFactory
{
    // TODO: consider moving this to another class
    public static ServiceBusMessage CreateServiceBusMessage(InboxEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var message = new ServiceBusMessage
        {
            Body = new BinaryData(@event.Message.ToByteArray()),
            Subject = @event.InboxEventName,
            MessageId = @event.InboxEventIdentification.ToString(),
        };

        message.ApplicationProperties.Add("RequestId", @event.InboxEventIdentification.ToString());

        return message;
    }

    public static AggregatedTimeSeriesRequestAccepted CreateResponseFromWholeSaleTemp(
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
        };

        return new Serie()
        {
            SettlementVersion = "2",
            GridArea = aggregatedMeasureDataProcess.MeteringGridAreaDomainId,
            Product = Product.Tarif,
            QuantityUnit = QuantityUnit.Kwh,
            Period = period,
            TimeSeriesPoints = { point },
        };
    }
}
