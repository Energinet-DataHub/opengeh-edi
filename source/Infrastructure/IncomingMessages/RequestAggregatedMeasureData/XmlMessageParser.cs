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

public class XmlMessageParser : IMessageParser<Serie, RequestAggregatedMeasureDataTransaction>
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

    public async Task<MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>> ParseAsync(Stream message, CancellationToken cancellationToken)
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
            return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>(
                new UnknownBusinessReasonOrVersion(businessProcessType, version));
        }

        ResetMessagePosition(message);
        using var reader = XmlReader.Create(message, CreateXmlReaderSettings(xmlSchema));

        if (_errors.Count > 0)
        {
            return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>(_errors.ToArray());
        }

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

    private static MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction> InvalidXmlFailure(
        Exception exception)
    {
        return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>(
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

    private static async Task<MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>> ParseXmlDataAsync(
        XmlReader reader, CancellationToken cancellationToken)
    {
        var root = await reader.ReadRootElementAsync().ConfigureAwait(false);
        var messageHeader = await MessageHeaderExtractor
            .ExtractAsync(reader, root, HeaderElementName, SeriesRecordElementName, cancellationToken).ConfigureAwait(false);

        var series = new List<Serie>();
        await foreach (var serie in ParseSerieAsync(reader, root))
        {
            series.Add(serie);
        }

        return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>(
            new RequestAggregatedMeasureDataIncomingMarketDocument(messageHeader, new List<Serie>()));
    }

    private static async IAsyncEnumerable<Serie> ParseSerieAsync(XmlReader reader, RootElement rootElement)
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

        await reader.AdvanceToAsync(SeriesRecordElementName, ns).ConfigureAwait(false);

        while (!reader.EOF)
        {
            if (reader.Is(SeriesRecordElementName, ns, XmlNodeType.EndElement))
            {
                var record = CreateSerie(
                    ref id);
                yield return record;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.SchemaInfo?.Validity == XmlSchemaValidity.Invalid)
                await reader.ReadToEndAsync().ConfigureAwait(false);

            if (reader.Is("mRID", ns))
            {
                id = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }

            // else if (reader.Is("marketEvaluationPoint.mRID", ns))
            // {
            //     marketEvaluationPointId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            // }
            // else if (reader.Is("marketEvaluationPoint.energySupplier_MarketParticipant.mRID", ns))
            // {
            //     energySupplierId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            // }
            // else if (reader.Is("marketEvaluationPoint.balanceResponsibleParty_MarketParticipant.mRID", ns))
            // {
            //     balanceResponsibleId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            // }
            // else if (reader.Is("marketEvaluationPoint.customer_MarketParticipant.mRID", ns))
            // {
            //     consumerIdType = reader.GetAttribute("codingScheme") ?? string.Empty;
            //     consumerId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            // }
            // else if (reader.Is("marketEvaluationPoint.customer_MarketParticipant.name", ns))
            // {
            //     consumerName = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            // }
            // else if (reader.Is("start_DateAndOrTime.dateTime", ns))
            // {
            //     effectiveDate = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            // }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }
    }

    private static Serie CreateSerie(ref string id)
    {
        var serie = new Serie(id)
        {
            Id = id,
        };

        id = string.Empty;

        return serie;
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
