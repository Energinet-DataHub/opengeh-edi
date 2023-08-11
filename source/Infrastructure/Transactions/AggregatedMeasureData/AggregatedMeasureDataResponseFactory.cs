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
using Domain.OutgoingMessages;
using Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.OutgoingMessages.Common;
using Period = Energinet.DataHub.Edi.Responses.Period;
using Serie = Energinet.DataHub.Edi.Responses.Serie;

namespace Infrastructure.Transactions.AggregatedMeasureData;

public static class AggregatedMeasureDataResponseFactory
{
    public static ServiceBusMessage CreateServiceBusMessage(AggregatedMeasureDataProcess process)
    {
        if (process == null) throw new ArgumentNullException(nameof(process));

        var bodyFromWholesaleMock = process.BusinessReason == CimCode.Of(BusinessReason.BalanceFixing)
            ? CreateRejectedResponseFromWholesale()
            : CreateAcceptedResponseFromWholesale(process);

        var message = new ServiceBusMessage()
        {
            Body = new BinaryData(bodyFromWholesaleMock.ToByteArray()),
            Subject = bodyFromWholesaleMock.GetType().Name,
            MessageId = process.ProcessId.Id.ToString(),
        };

        message.ApplicationProperties.Add("ReferenceId", process.ProcessId.Id.ToString());
        return message;
    }

    private static IMessage CreateRejectedResponseFromWholesale()
    {
        var wholesaleResponse = new AggregatedTimeSeriesRequestRejected();
        wholesaleResponse.RejectReasons.Add(CreateRejectReason());
        return wholesaleResponse;
    }

    private static RejectReason CreateRejectReason()
    {
        return new RejectReason()
        {
            ErrorMessage = "something went wrong",
            ErrorCode = ErrorCodes.InvalidBalanceResponsibleForPeriod,
        };
    }

    private static IMessage CreateAcceptedResponseFromWholesale(
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
            Time = new Timestamp() { Seconds = 1, },
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
            TimeSeriesType = TimeSeriesType.Production,
        };
    }
}
