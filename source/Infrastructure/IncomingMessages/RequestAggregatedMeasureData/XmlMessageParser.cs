using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Application.IncomingMessages.RequestAggregatedMeasureData;
using CimMessageAdapter.Errors;
using CimMessageAdapter.Messages;
using DocumentValidation;
using DocumentValidation.CimXml;
using DocumentFormat = Domain.Documents.DocumentFormat;

namespace Infrastructure.IncomingMessages.RequestAggregatedMeasureData;

public class XmlMessageParser : IMessageParser<Series, RequestAggregatedMeasureDataTransaction>
{
    private const string SeriesRecordElementName = "Series";
    private const string HeaderElementName = "RequestAggregatedMeasureData_MarketDocument";
    private readonly ISchemaProvider _schemaProvider;
    private readonly List<ValidationError> _errors = new();

    public XmlMessageParser()
    {
        _schemaProvider = new CimXmlSchemaProvider();
    }

    public DocumentFormat HandledFormat => DocumentFormat.Xml;

    public async Task<MessageParserResult<Series, RequestAggregatedMeasureDataTransaction>> ParseAsync(Stream message, CancellationToken cancellationToken)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        string version;
        string businessProcessType;
        try
        {
            version = GetVersion(message);
            businessProcessType = GetBusinessReason(message);
        }
        catch (XmlException exception)
        {
            return InvalidXmlFailure(exception);
        }
        catch (ObjectDisposedException generalException)
        {
            return InvalidXmlFailure(generalException);
        }

        var xmlSchema = await _schemaProvider.GetSchemaAsync<XmlSchema>(businessProcessType, version, cancellationToken)
            .ConfigureAwait(true);
        if (xmlSchema is null)
        {
            return new MessageParserResult<Series, RequestAggregatedMeasureDataTransaction>(
                new UnknownBusinessReasonOrVersion(businessProcessType, version));
        }

        ResetMessagePosition(message);
        using (var reader = XmlReader.Create(message, CreateXmlReaderSettings(xmlSchema)))
        {
            try
            {
                return await ParseXmlDataAsync(reader, cancellationToken).ConfigureAwait(false);
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

    private static MessageParserResult<Series, RequestAggregatedMeasureDataTransaction> InvalidXmlFailure(
        Exception exception)
    {
        return new MessageParserResult<Series, RequestAggregatedMeasureDataTransaction>(
            InvalidMessageStructure.From(exception));
    }

    private static string GetBusinessReason(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var split = SplitNamespace(message);
        var businessReason = split[3];
        return businessReason;
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

    private static async Task<MessageParserResult<Series, RequestAggregatedMeasureDataTransaction>> ParseXmlDataAsync(
        XmlReader reader, CancellationToken cancellationToken)
    {
        var root = await reader.ReadRootElementAsync().ConfigureAwait(false);
        var messageHeader = await MessageHeaderExtractor
            .ExtractAsync(reader, root, HeaderElementName, SeriesRecordElementName, cancellationToken).ConfigureAwait(false);
        // if (_errors.Count > 0)
        // {
        //     return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(_errors.ToArray());
        // }
        //
        // var marketActivityRecords = new List<MarketActivityRecord>();
        // await foreach (var marketActivityRecord in MarketActivityRecordsFromAsync(reader, root))
        // {
        //     marketActivityRecords.Add(marketActivityRecord);
        // }
        //
        // if (_errors.Count > 0)
        // {
        //     return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(_errors.ToArray());
        // }
        return new MessageParserResult<Series, RequestAggregatedMeasureDataTransaction>(
            new RequestAggregatedMeasureDataIncomingMarketDocument(messageHeader, new List<Series>()));
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
}
