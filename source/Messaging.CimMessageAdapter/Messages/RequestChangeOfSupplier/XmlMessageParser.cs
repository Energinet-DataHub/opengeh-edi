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
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Messaging.Application.SchemaStore;
using Messaging.CimMessageAdapter.Errors;
using Messaging.Domain.OutgoingMessages;
using MessageHeader = Messaging.Application.IncomingMessages.MessageHeader;

namespace Messaging.CimMessageAdapter.Messages.RequestChangeOfSupplier;

public class XmlMessageParser : IMessageParser<MarketActivityRecord>
{
    private const string MarketActivityRecordElementName = "MktActivityRecord";
    private const string HeaderElementName = "RequestChangeOfSupplier_MarketDocument";
    private readonly List<ValidationError> _errors = new();
    private readonly ISchemaProvider _schemaProvider;

    public XmlMessageParser()
    {
        _schemaProvider = new XmlSchemaProvider();
    }

    public CimFormat HandledFormat => CimFormat.Xml;

    public async Task<MessageParserResult> ParseAsync(Stream message)
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
            return MessageParserResult.Failure(new UnknownBusinessProcessTypeOrVersion(businessProcessType, version));
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
        var marketEvaluationPointId = string.Empty;
        var energySupplierId = string.Empty;
        var balanceResponsibleId = string.Empty;
        var consumerId = string.Empty;
        var consumerIdType = string.Empty;
        var consumerName = string.Empty;
        var effectiveDate = string.Empty;
        var ns = rootElement.DefaultNamespace;

        await reader.AdvanceToAsync(MarketActivityRecordElementName, ns).ConfigureAwait(false);

        while (!reader.EOF)
        {
            if (reader.Is(MarketActivityRecordElementName, ns, XmlNodeType.EndElement))
            {
                var record = CreateMarketActivityRecord(
                    ref id,
                    ref consumerName,
                    ref consumerId,
                    ref consumerIdType,
                    ref marketEvaluationPointId,
                    ref energySupplierId,
                    ref effectiveDate,
                    ref balanceResponsibleId);
                yield return record;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.SchemaInfo?.Validity == XmlSchemaValidity.Invalid)
                await reader.ReadToEndAsync().ConfigureAwait(false);

            if (reader.Is("mRID", ns))
            {
                id = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("marketEvaluationPoint.mRID", ns))
            {
                marketEvaluationPointId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("marketEvaluationPoint.energySupplier_MarketParticipant.mRID", ns))
            {
                energySupplierId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("marketEvaluationPoint.balanceResponsibleParty_MarketParticipant.mRID", ns))
            {
                balanceResponsibleId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("marketEvaluationPoint.customer_MarketParticipant.mRID", ns))
            {
                consumerIdType = reader.GetAttribute("codingScheme") ?? string.Empty;
                consumerId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("marketEvaluationPoint.customer_MarketParticipant.name", ns))
            {
                consumerName = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("start_DateAndOrTime.dateTime", ns))
            {
                effectiveDate = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }
    }

    private static MarketActivityRecord CreateMarketActivityRecord(
        ref string id,
        ref string consumerName,
        ref string consumerId,
        ref string consumerIdType,
        ref string marketEvaluationPointId,
        ref string energySupplierId,
        ref string effectiveDate,
        ref string balanceResponsibleId)
    {
        var marketActivityRecord = new MarketActivityRecord()
        {
            Id = id,
            ConsumerName = consumerName,
            ConsumerId = consumerId,
            ConsumerIdType = consumerIdType,
            MarketEvaluationPointId = marketEvaluationPointId,
            EnergySupplierId = energySupplierId,
            EffectiveDate = effectiveDate,
            BalanceResponsibleId =
                balanceResponsibleId,
        };

        id = string.Empty;
        marketEvaluationPointId = string.Empty;
        energySupplierId = string.Empty;
        balanceResponsibleId = string.Empty;
        consumerId = string.Empty;
        consumerIdType = string.Empty;
        consumerName = string.Empty;
        effectiveDate = string.Empty;
        return marketActivityRecord;
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

    private static MessageParserResult InvalidXmlFailure(Exception exception)
    {
        return MessageParserResult.Failure(InvalidMessageStructure.From(exception));
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

    private void OnValidationError(object? sender, ValidationEventArgs arguments)
    {
        var message =
            $"XML schema validation error at line {arguments.Exception.LineNumber}, position {arguments.Exception.LinePosition}: {arguments.Message}.";
        _errors.Add(InvalidMessageStructure.From(message));
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

    private async Task<MessageParserResult> ParseXmlDataAsync(XmlReader reader)
    {
        var root = await reader.ReadRootElementAsync().ConfigureAwait(false);
        var messageHeader = await ExtractMessageHeaderAsync(reader, root).ConfigureAwait(false);
        if (_errors.Count > 0)
        {
            return MessageParserResult.Failure(_errors.ToArray());
        }

        var marketActivityRecords = new List<MarketActivityRecord>();
        await foreach (var marketActivityRecord in MarketActivityRecordsFromAsync(reader, root))
        {
            marketActivityRecords.Add(marketActivityRecord);
        }

        if (_errors.Count > 0)
        {
            return MessageParserResult.Failure(_errors.ToArray());
        }

        return MessageParserResult.Succeeded(messageHeader, marketActivityRecords);
    }
}
