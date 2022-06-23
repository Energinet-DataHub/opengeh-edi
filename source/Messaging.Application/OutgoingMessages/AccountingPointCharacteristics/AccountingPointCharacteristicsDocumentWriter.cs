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
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using System.Xml;
using Messaging.Application.Common;

namespace Messaging.Application.OutgoingMessages.AccountingPointCharacteristics;

public class AccountingPointCharacteristicsDocumentWriter : DocumentWriter
{
    private const string Prefix = "cim";
    private const string DocumentType = "AccountingPointCharacteristics_MarketDocument";
    private const string XmlNamespace = "urn:ediel.org:structure:accountingpointcharacteristics:0:1";
    private const string SchemaLocation = "urn:ediel.org:structure:accountingpointcharacteristics:0:1 urn-ediel-org-structure-accountingpointcharacteristics-0-1.xsd";

    public AccountingPointCharacteristicsDocumentWriter(IMarketActivityRecordParser parser)
        : base(new DocumentDetails(DocumentType, SchemaLocation, XmlNamespace, Prefix), parser)
    {
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        if (marketActivityPayloads == null) throw new ArgumentNullException(nameof(marketActivityPayloads));
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        foreach (var marketActivityRecord in ParseFrom<MarketActivityRecord>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(Prefix, "MktActivityRecord", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "mRID", null, marketActivityRecord.Id.ToString()).ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "originalTransactionIDReference_MktActivityRecord.mRID", null, marketActivityRecord.OriginalTransactionId).ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "validityStart_DateAndOrTime.dateTime", null, marketActivityRecord.ValidityStartDate.ToString()).ConfigureAwait(false);
            await WriteMarketEvaluationPointAsync(marketActivityRecord.MarketEvaluationPt, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private static async Task WriteMarketEvaluationPointAsync(MarketEvaluationPoint marketEvaluationPoint, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(Prefix, "MarketEvaluationPoint", null).ConfigureAwait(false);

        await WriteMridAsync("mRID", marketEvaluationPoint.MRID, writer)
            .ConfigureAwait(false);

        await WriteMridAsync("meteringPointResponsible_MarketParticipant.mRID", marketEvaluationPoint.MeteringPointResponsible, writer).ConfigureAwait(false);

        await writer.WriteElementStringAsync(Prefix, "type", null, marketEvaluationPoint.Type).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "settlementMethod", null, marketEvaluationPoint.SettlementMethod).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "meteringMethod", null, marketEvaluationPoint.MeteringMethod).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "connectionState", null, marketEvaluationPoint.ConnectionState).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "readCycle", null, marketEvaluationPoint.ReadCycle).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "netSettlementGroup", null, marketEvaluationPoint.NetSettlementGroup).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "nextReadingDate", null, marketEvaluationPoint.NextReadingDate).ConfigureAwait(false);

        await WriteMridAsync("meteringGridArea_Domain.mRID", marketEvaluationPoint.MeteringGridAreaId, writer).ConfigureAwait(false);
        await WriteMridAsync("inMeteringGridArea_Domain.mRID", marketEvaluationPoint.InMeteringGridAreaId, writer).ConfigureAwait(false);
        await WriteMridAsync("outMeteringGridArea_Domain.mRID", marketEvaluationPoint.OutMeteringGridAreaId, writer).ConfigureAwait(false);
        await WriteUnitValueAsync("physicalConnectionCapacity", marketEvaluationPoint.PhysicalConnectionCapacity, writer).ConfigureAwait(false);

        await writer.WriteElementStringAsync(Prefix, "mPConnectionType", null, marketEvaluationPoint.ConnectionType).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "disconnectionMethod", null, marketEvaluationPoint.DisconnectionMethod).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "asset_MktPSRType.psrType", null, marketEvaluationPoint.PsrType).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "productionObligation", null, marketEvaluationPoint.ProductionObligation).ConfigureAwait(false);

        await WriteUnitValueAsync("contractedConnectionCapacity", marketEvaluationPoint.ContractedConnectionCapacity, writer).ConfigureAwait(false);
        await WriteUnitValueAsync("ratedCurrent", marketEvaluationPoint.RatedCurrent, writer).ConfigureAwait(false);

        await writer.WriteElementStringAsync(Prefix, "meter.mRID", null, marketEvaluationPoint.MeterId).ConfigureAwait(false);

        await writer.WriteStartElementAsync(Prefix, "Series", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "product", null, marketEvaluationPoint.Series.Product).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "quantity_Measure_Unit.name", null, marketEvaluationPoint.Series.QuantityMeasureUnit).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await WriteMridAsync("energySupplier_MarketParticipant.mRID", marketEvaluationPoint.EnergySupplier, writer).ConfigureAwait(false);

        await writer.WriteElementStringAsync(Prefix, "supplyStart_DateAndOrTime.dateTime", null, marketEvaluationPoint.SupplyStart.ToString()).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "description", null, marketEvaluationPoint.Description).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "usagePointLocation.geoInfoReference", null, marketEvaluationPoint.GeoInfoReference).ConfigureAwait(false);

        await WriteAddressAsync(marketEvaluationPoint.MainAddress, writer).ConfigureAwait(false);

        await writer.WriteElementStringAsync(Prefix, "usagePointLocation.actualAddressIndicator", null, marketEvaluationPoint.IsActualAddress).ConfigureAwait(false);

        await WriteParentMarketEvaluationPointAsync(marketEvaluationPoint.ParentMktEvaluationPoint, writer).ConfigureAwait(false);

        await WriteChildMarketEvaluationPointAsync(marketEvaluationPoint.ChildMktEvaluationPoint, writer).ConfigureAwait(false);

        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private static async Task WriteUnitValueAsync(string localName, UnitValue unitvalue, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(Prefix, localName, null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "unit", null, unitvalue.Unit).ConfigureAwait(false);
        writer.WriteValue(unitvalue.Value);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private static async Task WriteMridAsync(string localName, Mrid mrid, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(Prefix, localName, null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "codingScheme", null, mrid.CodingScheme).ConfigureAwait(false);
        writer.WriteValue(mrid.Id);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private static async Task WriteChildMarketEvaluationPointAsync(ChildMarketEvaluationPoint childMktEvaluationPoint, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(Prefix, "Child_MarketEvaluationPoint", null).ConfigureAwait(false);
        await WriteMridAsync("mRID", childMktEvaluationPoint.Id, writer).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "description", null, childMktEvaluationPoint.Description).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private static async Task WriteParentMarketEvaluationPointAsync(ParentMarketEvaluationPoint parentMktEvaluationPoint, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(Prefix, "Parent_MarketEvaluationPoint", null).ConfigureAwait(false);
        await WriteMridAsync("mRID", parentMktEvaluationPoint.Id, writer).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "description", null, parentMktEvaluationPoint.Description).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private static async Task WriteAddressAsync(Address address, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(Prefix, "usagePointLocation.mainAddress", null).ConfigureAwait(false);
        await WriteStreetDetailsAsync(address.Street, writer).ConfigureAwait(false);
        await WriteTownDetailsAsync(address.Town, writer).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private static async Task WriteStreetDetailsAsync(StreetDetail street, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(Prefix, "streetDetail", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "code", null, street.Code).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "name", null, street.Name).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "number", null, street.Number).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "floorIdentification", null, street.FloorIdentification).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "suiteNumber", null, street.SuiteNumber).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private static async Task WriteTownDetailsAsync(TownDetail town, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(Prefix, "townDetail", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "code", null, town.Code).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "name", null, town.Name).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "section", null, town.Section).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "country", null, town.Country).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }
}
