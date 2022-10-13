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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.OutgoingMessages.Common.Xml;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.SeedWork;
using Messaging.Infrastructure.OutgoingMessages.Common.Xml;

namespace Messaging.Infrastructure.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;

public class CharacteristicsOfACustomerAtAnApDocumentWriter : DocumentWriter
{
    private MessageHeader? _header;

    public CharacteristicsOfACustomerAtAnApDocumentWriter(IMarketActivityRecordParser parser)
        : base(
            new DocumentDetails(
            "CharacteristicsOfACustomerAtAnAP_MarketDocument",
            schemaLocation: "urn:ediel.org:structure:characteristicsofacustomeratanap:0:1 urn-ediel-org-structure-characteristicsofacustomeratanap-0-1.xsd",
            xmlNamespace: "urn:ediel.org:structure:characteristicsofacustomeratanap:0:1",
            prefix: "cim",
            typeCode: "E21"),
            parser)
    {
    }

    public override Task<Stream> WriteAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords)
    {
        _header = header;
        return base.WriteAsync(header, marketActivityRecords);
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        if (marketActivityPayloads == null) throw new ArgumentNullException(nameof(marketActivityPayloads));
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        foreach (var marketActivityRecord in ParseFrom<MarketActivityRecord>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MktActivityRecord", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "mRID", null, marketActivityRecord.Id).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "validityStart_DateAndOrTime.dateTime", null, marketActivityRecord.ValidityStart.ToString()).ConfigureAwait(false);
            await WriteMarketEvaluationPointAsync(marketActivityRecord.MarketEvaluationPoint, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteMarketEvaluationPointAsync(MarketEvaluationPoint marketEvaluationPoint, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MarketEvaluationPoint", null).ConfigureAwait(false);

        await WriteMridAsync("mRID", marketEvaluationPoint.MarketEvaluationPointId, "A10", writer).ConfigureAwait(false);
        await WriteElementAsync("serviceCategory.ElectricalHeating", marketEvaluationPoint.ElectricalHeating.ToStringValue(), writer).ConfigureAwait(false);
        await WriteElementAsync("eletricalHeating_DateAndOrTime.dateTime", marketEvaluationPoint.ElectricalHeatingStart?.ToString() ?? string.Empty, writer).ConfigureAwait(false);

        if (marketEvaluationPoint.FirstCustomerId.CodingScheme.Equals("VA", StringComparison.OrdinalIgnoreCase))
        {
            await WriteMridAsync("firstCustomer_MarketParticipant.mRID", marketEvaluationPoint.FirstCustomerId.Id, marketEvaluationPoint.FirstCustomerId.CodingScheme, writer).ConfigureAwait(false);
        }
        else
        {
            if (ReceiverIs(MarketRole.EnergySupplier))
            {
                await WriteMridAsync("firstCustomer_MarketParticipant.mRID", marketEvaluationPoint.FirstCustomerId.Id, marketEvaluationPoint.FirstCustomerId.CodingScheme, writer).ConfigureAwait(false);
            }
        }

        await WriteElementAsync("firstCustomer_MarketParticipant.name", marketEvaluationPoint.FirstCustomerName, writer).ConfigureAwait(false);

        await WriteIfReceiverRoleIsAsync(
                MarketRole.EnergySupplier,
                () => WriteMridAsync("secondCustomer_MarketParticipant.mRID", marketEvaluationPoint.SecondCustomerId.Id, marketEvaluationPoint.SecondCustomerId.CodingScheme, writer))
            .ConfigureAwait(false);

        await WriteElementAsync("secondCustomer_MarketParticipant.name", marketEvaluationPoint.SecondCustomerName, writer).ConfigureAwait(false);
        await WriteElementAsync("protectedName", marketEvaluationPoint.ProtectedName.ToStringValue(), writer).ConfigureAwait(false);
        await WriteElementAsync("hasEnergySupplier", marketEvaluationPoint.HasEnergySupplier.ToStringValue(), writer).ConfigureAwait(false);

        await WriteIfReceiverRoleIsAsync(
            MarketRole.EnergySupplier, () => WriteElementAsync("supplyStart_DateAndOrTime.dateTime", marketEvaluationPoint.SupplyStart.ToString(), writer))
            .ConfigureAwait(false);

        foreach (var usagePointLocation in marketEvaluationPoint.UsagePointLocation)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "UsagePointLocation", null).ConfigureAwait(false);
            await WriteElementAsync("type", usagePointLocation.Type, writer).ConfigureAwait(false);
            await WriteElementAsync("geoInfoReference", usagePointLocation.GeoInfoReference, writer).ConfigureAwait(false);
            await WriteMainAddressAsync(writer, usagePointLocation.MainAddress).ConfigureAwait(false);
            await WriteElementAsync("name", usagePointLocation.Name, writer).ConfigureAwait(false);
            await WriteElementAsync("attn_Names.name", usagePointLocation.AttnName, writer).ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "phone1", null).ConfigureAwait(false);
            await WriteElementAsync("ituPhone", usagePointLocation.Phone1, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "phone2", null).ConfigureAwait(false);
            await WriteElementAsync("ituPhone", usagePointLocation.Phone2, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "electronicAddress", null).ConfigureAwait(false);
            await WriteElementAsync("email1", usagePointLocation.EmailAddress, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await WriteElementAsync("protectedAddress", usagePointLocation.ProtectedAddress.ToStringValue(), writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private Task WriteIfReceiverRoleIsAsync(MarketRole marketRole, Func<Task> writeAction)
    {
        var receiverRole = EnumerationType
            .GetAll<MarketRole>()
            .FirstOrDefault(t => t.Name.Equals(_header?.ReceiverRole, StringComparison.OrdinalIgnoreCase));

        if (marketRole == receiverRole)
        {
            return writeAction();
        }

        return Task.CompletedTask;
    }

    private bool ReceiverIs(MarketRole marketRole)
    {
        var receiverRole = EnumerationType
            .GetAll<MarketRole>()
            .FirstOrDefault(t => t.Name.Equals(_header?.ReceiverRole, StringComparison.OrdinalIgnoreCase));

        if (receiverRole is null)
        {
            throw new InvalidOperationException("Invalid receiver market role");
        }

        return receiverRole == marketRole;
    }

    private async Task WriteMainAddressAsync(XmlWriter writer, MainAddress mainAddress)
    {
        await writer.WriteStartElementAsync(DocumentDetails.Prefix, "mainAddress", null).ConfigureAwait(false);

        await writer.WriteStartElementAsync(DocumentDetails.Prefix, "streetDetail", null).ConfigureAwait(false);
        await WriteElementAsync("code", mainAddress.StreetDetail.Code, writer).ConfigureAwait(false);
        await WriteElementAsync("name", mainAddress.StreetDetail.Name, writer).ConfigureAwait(false);
        await WriteElementAsync("number", mainAddress.StreetDetail.Number, writer).ConfigureAwait(false);
        await WriteElementAsync("floorIdentification", mainAddress.StreetDetail.FloorIdentification, writer).ConfigureAwait(false);
        await WriteElementAsync("suiteNumber", mainAddress.StreetDetail.SuiteNumber, writer).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(DocumentDetails.Prefix, "townDetail", null).ConfigureAwait(false);
        await WriteElementAsync("code", mainAddress.TownDetail.Code, writer).ConfigureAwait(false);
        await WriteElementAsync("name", mainAddress.TownDetail.Name, writer).ConfigureAwait(false);
        await WriteElementAsync("section", mainAddress.TownDetail.Section, writer).ConfigureAwait(false);
        await WriteElementAsync("country", mainAddress.TownDetail.Country, writer).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await WriteElementAsync("postalCode", mainAddress.PostalCode, writer).ConfigureAwait(false);
        await WriteElementAsync("poBox", mainAddress.PoBox, writer).ConfigureAwait(false);

        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }
}
