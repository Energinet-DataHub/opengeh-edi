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

public static class AggregatedMeasureDataRequestAcceptedMessageFactory
{
    public static ServiceBusMessage Create(
        Guid processId,
        string gridAreaCode)
    {
        var body = CreateAcceptedResponse(gridAreaCode);

        var message = new ServiceBusMessage
        {
            Body = new BinaryData(body.ToByteArray()),
            Subject = body.GetType().Name,
        };

        message.ApplicationProperties.Add("ReferenceId", processId.ToString());
        return message;
    }

    private static AggregatedTimeSeriesRequestAccepted CreateAcceptedResponse(string gridAreaCode)
    {
        var response = new AggregatedTimeSeriesRequestAccepted();
        var startOfPeriod = DateTime.Parse("2023-01-31T23:00Z", CultureInfo.InvariantCulture).ToUniversalTime().ToTimestamp();
        var endOfPeriod = DateTime.Parse("2023-02-28T23:00Z", CultureInfo.InvariantCulture).ToUniversalTime().ToTimestamp();
        var points = CreateTimeSeriesPoints(startOfPeriod, endOfPeriod);
        response.Series.Add(new Series
        {
            GridArea = gridAreaCode,
            QuantityUnit = QuantityUnit.Kwh,
            TimeSeriesPoints = { points },
            TimeSeriesType = TimeSeriesType.Production,
            Resolution = Resolution.Pt15M,
            CalculationResultVersion = 1,
            Period = new Period()
            {
                StartOfPeriod = startOfPeriod,
                EndOfPeriod = endOfPeriod,
            },
        });

        return response;
    }

    private static List<TimeSeriesPoint> CreateTimeSeriesPoints(Timestamp startOfPeriod, Timestamp endOfPeriod)
    {
        var result = new List<TimeSeriesPoint>();
        var currentTime = startOfPeriod;
        while (currentTime < endOfPeriod)
        {
            var point = new TimeSeriesPoint
            {
                Quantity = MapDecimalToDecimalValue(121043.602656m + currentTime.Seconds),
                Time = currentTime,
            };
            point.QuantityQualities.Add(QuantityQuality.Estimated);
            result.Add(point);
            currentTime = new Timestamp { Seconds = currentTime.Seconds + 900, };
        }

        return result;
    }

    private static DecimalValue MapDecimalToDecimalValue(decimal value)
    {
        var units = decimal.ToInt64(value);
        var nanoFactor = 1_000_000_000;
        return new DecimalValue() { Units = units, Nanos = decimal.ToInt32((value - units) * nanoFactor), };
    }
}
