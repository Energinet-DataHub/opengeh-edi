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

using System.Globalization;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Period = Energinet.DataHub.Edi.Responses.Period;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers.MessageFactories;

public static class WholesaleServiceRequestAcceptedMessageFactory
{
    public static ServiceBusMessage Create(
        Guid processId,
        string gridArea,
        string energySupplierNumber,
        string chargeOwnerNumber)
    {
        var body = CreateAcceptedResponse(gridArea, energySupplierNumber, chargeOwnerNumber);

        var message = new ServiceBusMessage
        {
            Body = new BinaryData(body.ToByteArray()),
            Subject = body.GetType().Name,
        };

        message.ApplicationProperties.Add("ReferenceId", processId.ToString());
        return message;
    }

    private static WholesaleServicesRequestAccepted CreateAcceptedResponse(
        string gridArea,
        string energySupplierId,
        string chargeOwnerNumber)
    {
        var response = new WholesaleServicesRequestAccepted();
        var points =
            new List<WholesaleServicesRequestSeries.Types.Point>()
            {
                new()
                {
                    Amount = MapDecimalToDecimalValue(121043.602656m),
                },
            };

        var wholesaleSeries = new WholesaleServicesRequestSeries
        {
            Period =
                new Period
                {
                    StartOfPeriod = DateTime.Parse("2023-01-31T23:00Z", CultureInfo.InvariantCulture).ToUniversalTime().ToTimestamp(),
                    EndOfPeriod = DateTime.Parse("2023-02-28T23:00Z", CultureInfo.InvariantCulture).ToUniversalTime().ToTimestamp(),
                },
            GridArea = gridArea,
            EnergySupplierId = energySupplierId,
            ChargeCode = "88888888",
            ChargeType = WholesaleServicesRequestSeries.Types.ChargeType.Fee,
            ChargeOwnerId = chargeOwnerNumber,
            Resolution = WholesaleServicesRequestSeries.Types.Resolution.Monthly,
            QuantityUnit = WholesaleServicesRequestSeries.Types.QuantityUnit.Kwh,
            Currency = WholesaleServicesRequestSeries.Types.Currency.Dkk,
            TimeSeriesPoints = { points },
            CalculationResultVersion = 1,
            CalculationType = WholesaleServicesRequestSeries.Types.CalculationType.WholesaleFixing,
        };

        response.Series.Add(wholesaleSeries);

        return response;
    }

    private static DecimalValue MapDecimalToDecimalValue(decimal value)
    {
        var units = decimal.ToInt64(value);
        var nanoFactor = 1_000_000_000;
        return new DecimalValue() { Units = units, Nanos = decimal.ToInt32((value - units) * nanoFactor), };
    }
}
