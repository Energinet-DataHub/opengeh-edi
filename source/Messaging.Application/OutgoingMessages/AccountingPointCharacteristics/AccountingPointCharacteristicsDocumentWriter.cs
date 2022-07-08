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
using Messaging.Application.Common;

namespace Messaging.Application.OutgoingMessages.AccountingPointCharacteristics;

public class AccountingPointCharacteristicsDocumentWriter : DocumentWriter
{
    public AccountingPointCharacteristicsDocumentWriter(IMarketActivityRecordParser parser)
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

    private async Task WriteUnitValueAsync(string localName, UnitValue unitvalue, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(DocumentDetails.Prefix, localName, null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "unit", null, unitvalue.Unit).ConfigureAwait(false);
        writer.WriteValue(unitvalue.Value);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private async Task WriteAddressAsync(Address address, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(DocumentDetails.Prefix, "usagePointLocation.mainAddress", null).ConfigureAwait(false);
        await WriteStreetDetailsAsync(address.Street, writer).ConfigureAwait(false);
        await WriteTownDetailsAsync(address.Town, writer).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "postalCode", null, address.PostalCode).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private async Task WriteStreetDetailsAsync(StreetDetail street, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(DocumentDetails.Prefix, "streetDetail", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "code", null, street.Code).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "name", null, street.Name).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "number", null, street.Number).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "floorIdentification", null, street.FloorIdentification).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "suiteNumber", null, street.SuiteNumber).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private async Task WriteTownDetailsAsync(TownDetail town, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(DocumentDetails.Prefix, "townDetail", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "code", null, town.Code).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "name", null, town.Name).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "section", null, town.Section).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "country", null, town.Country).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private Task WriteMridAsync(string localName, Mrid mrid, XmlWriter writer)
    {
        return WriteMridAsync(localName, mrid.Id, mrid.CodingScheme, writer);
    }

    private async Task WriteRelatedMarketEvaluationPointAsync(RelatedMarketEvaluationPoint childMktEvaluationPoint, string localName, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(DocumentDetails.Prefix, localName, null).ConfigureAwait(false);
        await WriteMridAsync("mRID", childMktEvaluationPoint.Id, writer).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "description", null, childMktEvaluationPoint.Description).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private async Task WriteMarketEvaluationPointAsync(MarketEvaluationPoint marketEvaluationPoint, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MarketEvaluationPoint", null)
            .ConfigureAwait(false);
        await WriteMridAsync("mRID", marketEvaluationPoint.MRID, writer).ConfigureAwait(false);
        if (marketEvaluationPoint.MeteringPointResponsible != null)
        {
            await WriteMridAsync(
                "meteringPointResponsible_MarketParticipant.mRID",
                marketEvaluationPoint.MeteringPointResponsible,
                writer).ConfigureAwait(false);
        }

        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "type", null, marketEvaluationPoint.Type).ConfigureAwait(false);
        if (marketEvaluationPoint.Type == "E17")
        {
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "settlementMethod", null, marketEvaluationPoint.SettlementMethod).ConfigureAwait(false);
        }

        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "meteringMethod", null, marketEvaluationPoint.MeteringMethod).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "connectionState", null, marketEvaluationPoint.ConnectionState).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "readCycle", null, marketEvaluationPoint.ReadCycle).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "netSettlementGroup", null, marketEvaluationPoint.NetSettlementGroup).ConfigureAwait(false);
        if (marketEvaluationPoint.NetSettlementGroup == "6")
        {
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "nextReadingDate", null, marketEvaluationPoint.NextReadingDate).ConfigureAwait(false);
        }

        await WriteMridAsync("meteringGridArea_Domain.mRID", marketEvaluationPoint.MeteringGridAreaId, writer).ConfigureAwait(false);
        if (marketEvaluationPoint.InMeteringGridAreaId != null)
        {
            await WriteMridAsync("inMeteringGridArea_Domain.mRID", marketEvaluationPoint.InMeteringGridAreaId, writer).ConfigureAwait(false);
        }

        if (marketEvaluationPoint.OutMeteringGridAreaId != null)
        {
            await WriteMridAsync("outMeteringGridArea_Domain.mRID", marketEvaluationPoint.OutMeteringGridAreaId, writer).ConfigureAwait(false);
        }

        await WriteUnitValueAsync("physicalConnectionCapacity", marketEvaluationPoint.PhysicalConnectionCapacity, writer).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "mPConnectionType", null, marketEvaluationPoint.ConnectionType).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "disconnectionMethod", null, marketEvaluationPoint.DisconnectionMethod).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "asset_MktPSRType.psrType", null, marketEvaluationPoint.PsrType).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "productionObligation", null, marketEvaluationPoint.ProductionObligation).ConfigureAwait(false);
        await WriteUnitValueAsync("contractedConnectionCapacity", marketEvaluationPoint.ContractedConnectionCapacity, writer).ConfigureAwait(false);
        await WriteUnitValueAsync("ratedCurrent", marketEvaluationPoint.RatedCurrent, writer).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "meter.mRID", null, marketEvaluationPoint.MeterId).ConfigureAwait(false);
        await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Series", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "product", null, marketEvaluationPoint.Series.Product).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "quantity_Measure_Unit.name", null, marketEvaluationPoint.Series.QuantityMeasureUnit).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
        await WriteMridAsync("energySupplier_MarketParticipant.mRID", marketEvaluationPoint.EnergySupplier, writer).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "supplyStart_DateAndOrTime.dateTime", null, marketEvaluationPoint.SupplyStart.ToString()).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "description", null, marketEvaluationPoint.Description).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "usagePointLocation.geoInfoReference", null, marketEvaluationPoint.GeoInfoReference).ConfigureAwait(false);
        await WriteAddressAsync(marketEvaluationPoint.MainAddress, writer).ConfigureAwait(false);
        await writer.WriteElementStringAsync(DocumentDetails.Prefix, "usagePointLocation.actualAddressIndicator", null, marketEvaluationPoint.IsActualAddress).ConfigureAwait(false);
        if (marketEvaluationPoint.ParentMarketEvaluationPoint != null)
        {
            await WriteRelatedMarketEvaluationPointAsync(marketEvaluationPoint.ParentMarketEvaluationPoint, "Parent_MarketEvaluationPoint", writer).ConfigureAwait(false);
        }

        if (marketEvaluationPoint.ChildMarketEvaluationPoint != null)
        {
            await WriteRelatedMarketEvaluationPointAsync(marketEvaluationPoint.ChildMarketEvaluationPoint, "Child_MarketEvaluationPoint", writer).ConfigureAwait(false);
        }

        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }
}
