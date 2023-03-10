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
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;
using Application.OutgoingMessages.Common;
using Application.OutgoingMessages.Common.Xml;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using Infrastructure.OutgoingMessages.Common;
using Infrastructure.OutgoingMessages.Common.Xml;
using Microsoft.EntityFrameworkCore.Query.Internal;
using NodaTime;
using Point = Domain.OutgoingMessages.NotifyAggregatedMeasureData.Point;

namespace Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;

public class NotifyAggregatedMeasureDataMessageWriter : MessageWriter
{
    private const string ActiveEnergy = "8716867000030";

    public NotifyAggregatedMeasureDataMessageWriter(IMessageRecordParser parser)
        : base(
            new DocumentDetails(
            "NotifyAggregatedMeasureData_MarketDocument",
            "urn:ediel.org:measure:notifyaggregatedmeasuredata:0:1 urn-ediel-org-measure-notifyaggregatedmeasuredata-0-1.xsd",
            "urn:ediel.org:measure:notifyaggregatedmeasuredata:0:1",
            "cim",
            "E31"),
            parser,
            null)
    {
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var timeSeries in ParseFrom<TimeSeries>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Series", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "mRID", null, timeSeries.TransactionId.ToString()).ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "marketEvaluationPoint.type", null, CimCode.Of(MeteringPointType.From(timeSeries.MeteringPointType))).ConfigureAwait(false);
            await WriteElementIfHasValueAsync("marketEvaluationPoint.settlementMethod", SettlementMethodToCode(timeSeries.SettlementType), writer).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "meteringGridArea_Domain.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "NDK").ConfigureAwait(false);
            await writer.WriteStringAsync(timeSeries.GridAreaCode).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            if (timeSeries.EnergySupplierNumber is not null)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "energySupplier_MarketParticipant.mRID", null).ConfigureAwait(false);
                await writer.WriteAttributeStringAsync(null, "codingScheme", null, ResolveActorCodingScheme(timeSeries.EnergySupplierNumber)).ConfigureAwait(false);
                await writer.WriteStringAsync(timeSeries.EnergySupplierNumber).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (timeSeries.BalanceResponsibleNumber is not null)
            {
                await writer
                    .WriteStartElementAsync(DocumentDetails.Prefix, "balanceResponsibleParty_MarketParticipant.mRID", null).ConfigureAwait(false);
                await writer.WriteAttributeStringAsync(null, "codingScheme", null, ResolveActorCodingScheme(timeSeries.BalanceResponsibleNumber)).ConfigureAwait(false);
                await writer.WriteStringAsync(timeSeries.BalanceResponsibleNumber).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "product", null, ActiveEnergy).ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "quantity_Measure_Unit.name", null, MeasureUnitTypeCodeFrom(timeSeries.MeasureUnitType)).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Period", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "resolution", null, ResolutionCodeFrom(timeSeries.Resolution)).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "timeInterval", null).ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "start", null, ParsePeriodDateFrom(timeSeries.Period.Start)).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "end", null, ParsePeriodDateFrom(timeSeries.Period.End)).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            foreach (var point in timeSeries.Point)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Point", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "position", null, point.Position.ToString(NumberFormatInfo.InvariantInfo)).ConfigureAwait(false);
                if (point.Quantity is not null)
                {
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "quantity", null, point.Quantity.ToString()!).ConfigureAwait(false);
                }

                await WriteQualityIfRequiredAsync(writer, point).ConfigureAwait(false);

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private static string ResolutionCodeFrom(string valueToParse)
    {
        var resolution = Resolution.From(valueToParse);
        if (resolution == Resolution.QuarterHourly)
            return "PT15M";
        if (resolution == Resolution.Hourly)
            return "PT1H";
        return valueToParse;
    }

    private static string MeasureUnitTypeCodeFrom(string valueToParse)
    {
        var measureUnitType = MeasurementUnit.From(valueToParse);
        if (measureUnitType == MeasurementUnit.Kwh)
            return "KWH";

        return valueToParse;
    }

    private static string? SettlementMethodToCode(string? value)
    {
        if (value is null)
            return null;

        var settlementType = SettlementType.From(value);

        if (settlementType == SettlementType.Flex)
        {
            return "D01";
        }

        if (settlementType == SettlementType.NonProfiled)
        {
            return "E02";
        }

        throw new InvalidOperationException("Invalid settlement type");
    }

    private static string ResolveActorCodingScheme(string energySupplierNumber)
    {
        return ActorNumber.IsGlnNumber(energySupplierNumber) ? "A10" : "A01";
    }

    private static string ParsePeriodDateFrom(Instant instant)
    {
        return instant.ToString("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture);
    }

    private Task WriteQualityIfRequiredAsync(XmlWriter writer, Point point)
    {
        if (point.Quality is null)
            return Task.CompletedTask;

        if (Quality.From(point.Quality) == Quality.Measured)
            return Task.CompletedTask;

        return writer.WriteElementStringAsync(DocumentDetails.Prefix, "quality", null, Quality.From(point.Quality).Code);
    }
}
