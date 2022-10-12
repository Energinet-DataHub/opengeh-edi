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
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Messaging.Application.IncomingMessages.RequestChangeCustomerCharacteristics;
using Messaging.Application.SchemaStore;
using Messaging.CimMessageAdapter.Errors;
using Messaging.CimMessageAdapter.Messages;
using Messaging.Domain.OutgoingMessages;
using MarketActivityRecord = Messaging.Application.IncomingMessages.RequestChangeCustomerCharacteristics.MarketActivityRecord;
using MessageHeader = Messaging.Application.IncomingMessages.MessageHeader;

namespace Messaging.Infrastructure.IncomingMessages.RequestChangeCustomerCharacteristics;

public class XmlMessageParser : IMessageParser<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>
{
    private const string MarketActivityRecordElementName = "MktActivityRecord";
    private const string HeaderElementName = "RequestChangeCustomerCharacteristics_MarketDocument";
    private readonly List<ValidationError> _errors = new();
    private readonly ISchemaProvider _schemaProvider;

    public XmlMessageParser()
    {
        _schemaProvider = new XmlSchemaProvider();
    }

    public CimFormat HandledFormat => CimFormat.Xml;

    public async Task<MessageParserResult<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>> ParseAsync(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        string version;
        string businessProcessType;
        try
        {
            version = GetVersion(message);
            businessProcessType = GetBusinessProcessType(message);
        }
        catch (XmlException exception)
        {
            return InvalidXmlFailure(exception);
        }
        catch (ObjectDisposedException generalException)
        {
            return InvalidXmlFailure(generalException);
        }

        var xmlSchema = await _schemaProvider.GetSchemaAsync<XmlSchema>(businessProcessType, version)
            .ConfigureAwait(true);
        if (xmlSchema is null)
        {
            return new
                MessageParserResult<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>(
                    new UnknownBusinessProcessTypeOrVersion(businessProcessType, version));
        }

        ResetMessagePosition(message);
        using (var reader = XmlReader.Create(message, CreateXmlReaderSettings(xmlSchema)))
        {
            try
            {
                return await ParseXmlDataAsync(reader).ConfigureAwait(false);
            }
            catch (XmlException exception)
            {
                return InvalidXmlFailure(exception);
            }
            catch (ObjectDisposedException generalException)
            {
                return InvalidXmlFailure(generalException);
            }
        }
    }

    private static MessageParserResult<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>
        InvalidXmlFailure(Exception exception)
    {
        return new MessageParserResult<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>(
            InvalidMessageStructure.From(exception));
    }

    private static void ResetMessagePosition(Stream message)
    {
        if (message.CanRead && message.Position > 0)
            message.Position = 0;
    }

    private static string GetVersion(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var split = SplitNamespace(message);
        var version = split[4] + "." + split[5];
        return version;
    }

    private static string[] SplitNamespace(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        ResetMessagePosition(message);
        using var reader = XmlReader.Create(message);

        var split = Array.Empty<string>();
        while (reader.Read())
        {
            if (string.IsNullOrEmpty(reader.NamespaceURI)) continue;
            var @namespace = reader.NamespaceURI;
            split = @namespace.Split(':');
            break;
        }

        return split;
    }

    private static string GetBusinessProcessType(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var split = SplitNamespace(message);
        var processType = split[3];
        return processType;
    }

    private static async IAsyncEnumerable<MarketActivityRecord> MarketActivityRecordsFromAsync(
        XmlReader reader,
        RootElement rootElement)
    {
        var id = string.Empty;
        var effectiveDate = string.Empty;
        MarketEvaluationPoint? marketEvaluationPoint = null;
        var ns = rootElement.DefaultNamespace;

        await reader.AdvanceToAsync(MarketActivityRecordElementName, ns).ConfigureAwait(false);

        while (!reader.EOF)
        {
            if (reader.Is(MarketActivityRecordElementName, ns, XmlNodeType.EndElement))
            {
                var record = CreateMarketActivityRecord(
                    id,
                    effectiveDate,
                    marketEvaluationPoint!);
                yield return record;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.SchemaInfo?.Validity == XmlSchemaValidity.Invalid)
                await reader.ReadToEndAsync().ConfigureAwait(false);

            if (reader.Is("mRID", ns))
            {
                id = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("start_DateAndOrTime.dateTime", ns))
            {
                effectiveDate = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("MarketEvaluationPoint", ns))
            {
                marketEvaluationPoint = await ReadMarketEvaluationPointAsync(reader, ns).ConfigureAwait(false);
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task<MarketEvaluationPoint> ReadMarketEvaluationPointAsync(XmlReader reader, string ns)
    {
        var marketEvaluationPointId = string.Empty;
        var marketEvalationPointElectricalHeaing = false;
        var firstCustomerId = string.Empty;
        var firstCustomerName = string.Empty;
        var secondCustomerId = string.Empty;
        var secondCustomerName = string.Empty;
        var protectedName = false;
        PointLocation? firstPointLocation = null;
        PointLocation? secondPointLocation = null;

        var firstLocationReached = false;

        while (!reader.Is("MarketEvaluationPoint", ns, XmlNodeType.EndElement))
        {
            if (reader.Is("mRID", ns))
            {
                marketEvaluationPointId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("serviceCategory.ElectricalHeating", ns))
            {
                marketEvalationPointElectricalHeaing = bool.Parse(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
            }
            else if (reader.Is("firstCustomer_MarketParticipant.mRID", ns))
            {
                firstCustomerId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("firstCustomer_MarketParticipant.name", ns))
            {
                firstCustomerName = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("secondCustomer_MarketParticipant.mRID", ns))
            {
                secondCustomerId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("secondCustomer_MarketParticipant.name", ns))
            {
                secondCustomerName = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("protectedName", ns))
            {
                protectedName = bool.Parse(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
            }
            else if (reader.Is("UsagePointLocation", ns) && !firstLocationReached)
            {
                firstPointLocation = await ReadLocationPointAsync(reader, ns).ConfigureAwait(false);
                firstLocationReached = true;
            }
            else if (reader.Is("UsagePointLocation", ns))
            {
                secondPointLocation = await ReadLocationPointAsync(reader, ns).ConfigureAwait(false);
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }

        return new MarketEvaluationPoint(
            marketEvaluationPointId,
            marketEvalationPointElectricalHeaing,
            new Customer(firstCustomerId, firstCustomerName),
            new Customer(secondCustomerId, secondCustomerName),
            protectedName,
            firstPointLocation!,
            secondPointLocation!);
    }

    private static MarketActivityRecord CreateMarketActivityRecord(
        string id,
        string effectiveDate,
        MarketEvaluationPoint marketEvaluationPoint)
    {
        var marketActivityRecord = new MarketActivityRecord(
            id,
            effectiveDate,
            marketEvaluationPoint);

        return marketActivityRecord;
    }

    private static async Task<TownDetails> ReadTownDetailsAsync(XmlReader reader, string ns)
    {
        var marketEvaluationPointTownCode = string.Empty;
        var marketEvaluationPointTownName = string.Empty;
        var marketEvaluationPointTownSection = string.Empty;
        var marketEvaluationPointTownCountry = string.Empty;

        while (!reader.Is("townDetail", ns, XmlNodeType.EndElement))
        {
            if (reader.Is("code", ns))
            {
                marketEvaluationPointTownCode = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("name", ns))
            {
                marketEvaluationPointTownName = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("section", ns))
            {
                marketEvaluationPointTownSection = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("country", ns))
            {
                marketEvaluationPointTownCountry = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }

        return new TownDetails(
            marketEvaluationPointTownCode,
            marketEvaluationPointTownName,
            marketEvaluationPointTownSection,
            marketEvaluationPointTownCountry);
    }

    private static async Task<StreetDetails> ReadStreetDetailsAsync(XmlReader reader, string ns)
    {
        var marketEvaluationPointStreetCode = string.Empty;
        var marketEvaluationPointStreetName = string.Empty;
        var marketEvaluationPointStreetNumber = string.Empty;
        var marketEvaluationPointStreetFloorIdentification = string.Empty;
        var marketEvaluationPointStreetSuiteNumber = string.Empty;

        while (!reader.Is("streetDetail", ns, XmlNodeType.EndElement))
        {
            if (reader.Is("code", ns))
            {
                marketEvaluationPointStreetCode = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("name", ns))
            {
                marketEvaluationPointStreetName = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("number", ns))
            {
                marketEvaluationPointStreetNumber = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("floorIdentification", ns))
            {
                marketEvaluationPointStreetFloorIdentification = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("suiteNumber", ns))
            {
                marketEvaluationPointStreetSuiteNumber = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }

        return new StreetDetails(
            marketEvaluationPointStreetCode,
            marketEvaluationPointStreetName,
            marketEvaluationPointStreetNumber,
            marketEvaluationPointStreetFloorIdentification,
            marketEvaluationPointStreetSuiteNumber);
    }

    private static async Task<PointLocation> ReadLocationPointAsync(XmlReader reader, string ns)
    {
        var locationPointType = string.Empty;
        var locationPointGeoInfoReference = string.Empty;
        Address? address = null;
        var protectedAddress = false;
        var name = string.Empty;
        var attnName = string.Empty;

        while (!reader.Is("UsagePointLocation", ns, XmlNodeType.EndElement))
        {
            if (reader.Is("type", ns))
            {
                locationPointType = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("geoInfoReference", ns))
            {
                locationPointGeoInfoReference = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("mainAddress", ns))
            {
                address = await ReadAddressAsync(reader, ns).ConfigureAwait(false);
            }
            else if (reader.Is("protectedAddress", ns))
            {
                protectedAddress = bool.Parse(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
            }
            else if (reader.Is("name", ns))
            {
                name = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("attn_Names.name", ns))
            {
                attnName = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }

        return new PointLocation(
            locationPointType,
            locationPointGeoInfoReference,
            address!,
            protectedAddress,
            name,
            attnName);
    }

    private static async Task<Address> ReadAddressAsync(XmlReader reader, string ns)
    {
        var marketEvaluationPointAdressPostalCode = string.Empty;
        var marketEvaluationPointAdressPoBox = string.Empty;
        TownDetails? townDetails = null;
        StreetDetails? streetDetails = null;

        while (!reader.Is("mainAddress", ns, XmlNodeType.EndElement))
        {
            if (reader.Is("townDetail", ns))
            {
                townDetails = await ReadTownDetailsAsync(reader, ns).ConfigureAwait(false);
            }
            else if (reader.Is("streetDetail", ns))
            {
                streetDetails = await ReadStreetDetailsAsync(reader, ns).ConfigureAwait(false);
            }
            else if (reader.Is("postalCode", ns))
            {
                marketEvaluationPointAdressPostalCode = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("poBox", ns))
            {
                marketEvaluationPointAdressPoBox = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }

        return new Address(
            streetDetails!,
            townDetails!,
            marketEvaluationPointAdressPostalCode,
            marketEvaluationPointAdressPoBox);
    }

    private static async Task<MessageHeader> ExtractMessageHeaderAsync(
        XmlReader reader,
        RootElement rootElement)
    {
        var messageId = string.Empty;
        var processType = string.Empty;
        var senderId = string.Empty;
        var senderRole = string.Empty;
        var receiverId = string.Empty;
        var receiverRole = string.Empty;
        var createdAt = string.Empty;
        var ns = rootElement.DefaultNamespace;

        await reader.AdvanceToAsync(HeaderElementName, rootElement.DefaultNamespace).ConfigureAwait(false);

        while (!reader.EOF)
        {
            if (reader.Is("mRID", ns))
                messageId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is("process.processType", ns))
                processType = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is("sender_MarketParticipant.mRID", ns))
                senderId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is("sender_MarketParticipant.marketRole.type", ns))
                senderRole = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is("receiver_MarketParticipant.mRID", ns))
                receiverId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is("receiver_MarketParticipant.marketRole.type", ns))
                receiverRole = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else if (reader.Is("createdDateTime", ns))
                createdAt = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            else await reader.ReadAsync().ConfigureAwait(false);

            if (reader.Is(MarketActivityRecordElementName, ns)) break;
        }

        return new MessageHeader(messageId, processType, senderId, senderRole, receiverId, receiverRole, createdAt);
    }

    private XmlReaderSettings CreateXmlReaderSettings(XmlSchema xmlSchema)
    {
        var settings = new XmlReaderSettings
        {
            Async = true,
            ValidationType = ValidationType.Schema,
            ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                              XmlSchemaValidationFlags.ReportValidationWarnings,
        };

        settings.Schemas.Add(xmlSchema);
        settings.ValidationEventHandler += OnValidationError;
        return settings;
    }

    private void OnValidationError(object? sender, ValidationEventArgs arguments)
    {
        var message =
            $"XML schema validation error at line {arguments.Exception.LineNumber}, position {arguments.Exception.LinePosition}: {arguments.Message}.";
        _errors.Add(InvalidMessageStructure.From(message));
    }

    private async
        Task<MessageParserResult<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>>
        ParseXmlDataAsync(XmlReader reader)
    {
        var root = await reader.ReadRootElementAsync().ConfigureAwait(false);
        var messageHeader = await ExtractMessageHeaderAsync(reader, root).ConfigureAwait(false);
        if (_errors.Count > 0)
        {
            return new
                MessageParserResult<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>(
                    _errors.ToArray());
        }

        var marketActivityRecords = new List<MarketActivityRecord>();

        await foreach (var marketActivityRecord in MarketActivityRecordsFromAsync(reader, root))
        {
            marketActivityRecords.Add(marketActivityRecord);
        }

        if (_errors.Count > 0)
        {
            return new
                MessageParserResult<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>(
                    _errors.ToArray());
        }

        return new MessageParserResult<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>(
            new RequestChangeCustomerCharacteristicIncomingDocument(messageHeader, marketActivityRecords));
    }
}
