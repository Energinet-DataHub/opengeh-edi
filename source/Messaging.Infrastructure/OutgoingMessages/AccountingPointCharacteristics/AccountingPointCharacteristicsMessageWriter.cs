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
using System.Threading.Tasks;
using System.Xml;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.OutgoingMessages.Common.Xml;
using Messaging.Domain.OutgoingMessages.AccountingPointCharacteristics;
using Messaging.Domain.OutgoingMessages.AccountingPointCharacteristics.MarketEvaluationPointDetails;
using Messaging.Infrastructure.OutgoingMessages.Common.Xml;

namespace Messaging.Infrastructure.OutgoingMessages.AccountingPointCharacteristics;

public class AccountingPointCharacteristicsMessageWriter : MessageWriter
{
    public AccountingPointCharacteristicsMessageWriter(IMessageRecordParser parser)
        : base(
            new DocumentDetails(
                "AccountingPointCharacteristics_MarketDocument",
                "urn:ediel.org:structure:accountingpointcharacteristics:0:1 urn-ediel-org-structure-accountingpointcharacteristics-0-1.xsd",
                "urn:ediel.org:structure:accountingpointcharacteristics:0:1",
                "cim",
                "E07"),
            parser)
    {
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        if (marketActivityPayloads == null) throw new ArgumentNullException(nameof(marketActivityPayloads));
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        foreach (var marketActivityRecord in ParseFrom<MarketActivityRecord>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MktActivityRecord", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "mRID", null, marketActivityRecord.Id.ToString()).ConfigureAwait(false);
            if (marketActivityRecord.OriginalTransactionId != null)
            {
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "originalTransactionIDReference_MktActivityRecord.mRID", null, marketActivityRecord.OriginalTransactionId).ConfigureAwait(false);
            }

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "validityStart_DateAndOrTime.dateTime", null, marketActivityRecord.ValidityStartDate.ToString()).ConfigureAwait(false);
            await WriteMarketEvaluationPointAsync(marketActivityRecord.MarketEvaluationPt, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteUnitValueAsync(string localName, UnitValue? unitValue, XmlWriter writer)
    {
        if (unitValue != null)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, localName, null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "unit", null, unitValue.Unit).ConfigureAwait(false);
            writer.WriteValue(unitValue.Value);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteAddressAsync(Address? address, XmlWriter writer)
    {
        if (address != null)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "usagePointLocation.mainAddress", null)
                .ConfigureAwait(false);
            await WriteStreetDetailsAsync(address.Street, writer).ConfigureAwait(false);
            await WriteTownDetailsAsync(address.Town, writer).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "postalCode", null, address.PostalCode)
                .ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteStreetDetailsAsync(StreetDetail? street, XmlWriter writer)
    {
        if (street != null)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "streetDetail", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "code", null, street.Code).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "name", null, street.Name).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "number", null, street.Number).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "floorIdentification", null, street.FloorIdentification).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "suiteNumber", null, street.SuiteNumber).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteTownDetailsAsync(TownDetail? town, XmlWriter writer)
    {
        if (town != null)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "townDetail", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "code", null, town.Code).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "name", null, town.Name).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "section", null, town.Section).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "country", null, town.Country).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteMridAsync(string localName, Mrid? mrid, XmlWriter writer)
    {
        if (mrid != null)
        {
            await WriteMridAsync(localName, mrid.Id, mrid.CodingScheme, writer).ConfigureAwait(false);
        }
    }

    private async Task WriteRelatedMarketEvaluationPointAsync(RelatedMarketEvaluationPoint? relatedMktEvaluationPoint, string localName, XmlWriter writer)
    {
        if (relatedMktEvaluationPoint != null)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, localName, null).ConfigureAwait(false);
            await WriteMridAsync("mRID", relatedMktEvaluationPoint.Id, writer).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "description", null, relatedMktEvaluationPoint.Description).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteSeriesAsync(Series? series, XmlWriter writer)
    {
        if (series != null)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Series", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "product", null, series.Product).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "quantity_Measure_Unit.name", null, series.QuantityMeasureUnit).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteElementStringAsync(string localName, string? value, XmlWriter writer)
    {
        if (value != null)
        {
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, localName, null, value).ConfigureAwait(false);
        }
    }

    private async Task WriteMarketEvaluationPointAsync(MarketEvaluationPoint marketEvaluationPoint, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MarketEvaluationPoint", null).ConfigureAwait(false);
        await WriteMridAsync("mRID", marketEvaluationPoint.MRID, writer).ConfigureAwait(false);
        await WriteMridAsync("meteringPointResponsible_MarketParticipant.mRID", marketEvaluationPoint.MeteringPointResponsible, writer).ConfigureAwait(false);

        await WriteElementStringAsync("type", marketEvaluationPoint.Type, writer).ConfigureAwait(false);
        if (marketEvaluationPoint.Type == "E17")
        {
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "settlementMethod", null, marketEvaluationPoint.SettlementMethod).ConfigureAwait(false);
        }

        await WriteElementStringAsync("meteringMethod", marketEvaluationPoint.MeteringMethod, writer).ConfigureAwait(false);
        await WriteElementStringAsync("connectionState", marketEvaluationPoint.ConnectionState, writer).ConfigureAwait(false);
        await WriteElementStringAsync("readCycle", marketEvaluationPoint.ReadCycle, writer).ConfigureAwait(false);
        await WriteElementStringAsync("netSettlementGroup", marketEvaluationPoint.NetSettlementGroup, writer).ConfigureAwait(false);
        await WriteElementStringAsync("nextReadingDate", marketEvaluationPoint.NextReadingDate.Date, writer).ConfigureAwait(false);

        await WriteMridAsync("meteringGridArea_Domain.mRID", marketEvaluationPoint.MeteringGridAreaId, writer).ConfigureAwait(false);
        await WriteMridAsync("inMeteringGridArea_Domain.mRID", marketEvaluationPoint.InMeteringGridAreaId, writer).ConfigureAwait(false);
        await WriteMridAsync("outMeteringGridArea_Domain.mRID", marketEvaluationPoint.OutMeteringGridAreaId, writer).ConfigureAwait(false);

        await WriteMridAsync("linked_MarketEvaluationPoint.mRID", marketEvaluationPoint.LinkedMarketEvaluationPointId, writer).ConfigureAwait(false);
        await WriteUnitValueAsync("physicalConnectionCapacity", marketEvaluationPoint.PhysicalConnectionCapacity, writer).ConfigureAwait(false);
        await WriteElementStringAsync("mPConnectionType", marketEvaluationPoint.ConnectionType, writer).ConfigureAwait(false);
        await WriteElementStringAsync("disconnectionMethod", marketEvaluationPoint.DisconnectionMethod, writer).ConfigureAwait(false);
        await WriteElementStringAsync("asset_MktPSRType.psrType", marketEvaluationPoint.PsrType, writer).ConfigureAwait(false);
        await WriteElementStringAsync("productionObligation", marketEvaluationPoint.ProductionObligation, writer).ConfigureAwait(false);
        await WriteUnitValueAsync("contractedConnectionCapacity", marketEvaluationPoint.ContractedConnectionCapacity, writer).ConfigureAwait(false);
        await WriteUnitValueAsync("ratedCurrent", marketEvaluationPoint.RatedCurrent, writer).ConfigureAwait(false);
        await WriteElementStringAsync("meter.mRID", marketEvaluationPoint.MeterId, writer).ConfigureAwait(false);

        await WriteSeriesAsync(marketEvaluationPoint.Series, writer).ConfigureAwait(false);

        await WriteMridAsync("energySupplier_MarketParticipant.mRID", marketEvaluationPoint.EnergySupplier, writer).ConfigureAwait(false);
        await WriteElementStringAsync("supplyStart_DateAndOrTime.dateTime", marketEvaluationPoint.SupplyStart.ToString(), writer).ConfigureAwait(false);
        await WriteElementStringAsync("description", marketEvaluationPoint.Description, writer).ConfigureAwait(false);
        await WriteElementStringAsync("usagePointLocation.geoInfoReference", marketEvaluationPoint.GeoInfoReference, writer).ConfigureAwait(false);
        await WriteAddressAsync(marketEvaluationPoint.MainAddress, writer).ConfigureAwait(false);
        await WriteElementStringAsync("usagePointLocation.actualAddressIndicator", marketEvaluationPoint.IsActualAddress, writer).ConfigureAwait(false);
        await WriteRelatedMarketEvaluationPointAsync(marketEvaluationPoint.ParentMarketEvaluationPoint, "Parent_MarketEvaluationPoint", writer).ConfigureAwait(false);
        await WriteRelatedMarketEvaluationPointAsync(marketEvaluationPoint.ChildMarketEvaluationPoint, "Child_MarketEvaluationPoint", writer).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }
}
