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

using System.Xml.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM015;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Cim.Json;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.MessageParsers;

public sealed class RequestMeasurementsJsonMessageParserTests
{
    private static readonly string PathToMessages =
        $"MessageParsers{Path.DirectorySeparatorChar}";

    private static readonly string SubPath =
        $"{Path.DirectorySeparatorChar}RequestValidatedMeasurements{Path.DirectorySeparatorChar}";

    private readonly Dictionary<DocumentFormat, IMessageParser> _marketMessageParser = new()
    {
        // {
        //     DocumentFormat.Ebix,
        //     new RequestValidatedMeasurementsEbixMessageParser(new EbixSchemaProvider(), new Logger<RequestValidatedMeasurementsEbixMessageParser>(new LoggerFactory()))
        // },
        // {
        //     DocumentFormat.Xml,
        //     new RequestValidatedMeasurementsXmlMessageParser(new CimXmlSchemaProvider(new CimXmlSchemas()))
        // },
        {
            DocumentFormat.Json,
            new RequestMeasurementsJsonMessageParser(
                new JsonSchemaProvider(
                    new CimJsonSchemas()),
                new Logger<RequestMeasurementsJsonMessageParser>(new LoggerFactory()))
        },
    };

    public static TheoryData<DocumentFormat, Stream> CreateMessagesWithSingleAndMultipleTransactions()
    {
        var data = new TheoryData<DocumentFormat, Stream>
        {
            // { DocumentFormat.Ebix, CreateBaseEbixMessage("ValidRequestValidatedMeasurements.xml") },
            // { DocumentFormat.Ebix, CreateBaseEbixMessage("ValidRequestValidatedMeasurementsWithTwoTransactions.xml") },
            // { DocumentFormat.Ebix, CreateBaseEbixMessage("ValidPT1HRequestValidatedMeasurements.xml") },
            // { DocumentFormat.Xml, CreateBaseXmlMessage("ValidRequestValidatedMeasurements.xml") },
            // { DocumentFormat.Xml, CreateBaseXmlMessage("ValidRequestValidatedMeasurementsWithTwoTransactions.xml") },
            { DocumentFormat.Json, CreateBaseJsonMessage("ValidRequestValidatedMeasurements.json") },
            { DocumentFormat.Json, CreateBaseJsonMessage("ValidRequestValidatedMeasurementsWithTwoTransactions.json") },
        };

        return data;
    }

    public static TheoryData<DocumentFormat, Stream, string, string> CreateBadMessages()
    {
        var data = new TheoryData<DocumentFormat, Stream, string, string>
        {
            // { DocumentFormat.Ebix, CreateBaseEbixMessage("BadVersionRequestValidatedMeasurements.xml"), nameof(InvalidBusinessReasonOrVersion), "Schema version bad-version" },
            // { DocumentFormat.Ebix, CreateBaseEbixMessage("InvalidRequestValidatedMeasurements.xml"), nameof(InvalidMessageStructure), "invalid child element" },
            // { DocumentFormat.Xml, CreateBaseXmlMessage("BadVersionRequestValidatedMeasurements.xml"), nameof(InvalidBusinessReasonOrVersion), "Schema version bad" },
            // { DocumentFormat.Xml, CreateBaseXmlMessage("InvalidRequestValidatedMeasurements.xml"), nameof(InvalidMessageStructure), "invalid child element" },
            // { DocumentFormat.Xml, CreateBaseXmlMessage("InvalidRequestValidatedMeasurementsMissingRegistration.xml"), nameof(InvalidMessageStructure), "elements expected: 'registration_DateAndOrTime.dateTime'" },
            { DocumentFormat.Json, CreateBaseJsonMessage("InvalidRequestValidatedMeasurements.json"), nameof(InvalidMessageStructure), "[required, Required properties [\"sender_MarketParticipant.mRID\"] are not present]" },
            { DocumentFormat.Json, CreateBaseJsonMessage("InvalidRequestValidatedMeasurementsMarketEvaluationPointValue.json"), nameof(InvalidMessageStructure), "[required, Required properties [\"value\"] are not present]" },
        };

        return data;
    }

    [Theory]
    [MemberData(nameof(CreateMessagesWithSingleAndMultipleTransactions))]
    public async Task Given_RequestMeasurementsStream_When_ParseAsync_Then_SuccessfullyParsed(
        DocumentFormat format,
        Stream message)
    {
        var result = await _marketMessageParser.GetValueOrDefault(format)!.ParseAsync(
            new IncomingMarketMessageStream(message),
            CancellationToken.None);

        using var assertionScope = new AssertionScope();
        result.Success.Should().BeTrue();

        var marketMessage = (RequestMeasurementsMessageBase)result.IncomingMessage!;
        marketMessage.Should().NotBeNull();
        marketMessage.MessageId.Should().Be("111131835");
        marketMessage.MessageType.Should().Be("E73");
        marketMessage.CreatedAt.Should().Be("2022-12-17T09:30:47Z");
        marketMessage.SenderNumber.Should().Be("5799999933318");
        marketMessage.ReceiverNumber.Should().Be("5790001330552");
        marketMessage.SenderRoleCode.Should().Be("DDQ");
        marketMessage.BusinessReason.Should().Be("E23");
        marketMessage.ReceiverRoleCode.Should().Be("DGL");
        marketMessage.BusinessType.Should().Be("23");

        foreach (var series in marketMessage.Series.Cast<RequestMeasurementsSeries>())
        {
            series.Should().NotBeNull();
            series.TransactionId.Should()
                .Match(transactionId => transactionId == "1568914" || transactionId == "1568915");
            series.StartDateTime.Should()
                .Match(startDate => startDate == "2022-07-17T22:00:00Z" || startDate == "2022-07-17T22:00:00Z");
            series.EndDateTime.Should()
                .Match(endDate => endDate == "2022-08-17T22:00:00Z" || endDate == "2022-08-17T22:00:00Z");
            series.MeteringPointId.Value.Should().Be("579999993331812345");
            series.GridArea.Should().BeNull();
        }
    }

    [Theory]
    [MemberData(nameof(CreateBadMessages))]
    public async Task Given_InvalidRequestMeasurements_When_ParseAsync_Then_ResultsContainsExpectedErrorMessage(
        DocumentFormat format,
        Stream message,
        string expectedErrorType,
        string expectedErrorMessageContains)
    {
        var result = await _marketMessageParser.GetValueOrDefault(format)!.ParseAsync(
            new IncomingMarketMessageStream(message),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        expectedErrorType.Should().NotBeNull();
        result.Errors.Should().Contain(error => error.GetType().Name == expectedErrorType && error.Message.Contains(expectedErrorMessageContains));
    }

    private static MemoryStream CreateBaseEbixMessage(string fileName)
    {
        var xmlDocument = XDocument.Load(
            $"{PathToMessages}ebix{SubPath}{fileName}");

        var stream = new MemoryStream();
        xmlDocument.Save(stream);

        return stream;
    }

    private static MemoryStream CreateBaseXmlMessage(string fileName)
    {
        var xmlDocument = XDocument.Load(
            $"{PathToMessages}xml{SubPath}{fileName}");

        var stream = new MemoryStream();
        xmlDocument.Save(stream);

        return stream;
    }

    private static FileStream CreateBaseJsonMessage(string fileName)
    {
        return new FileStream($"{PathToMessages}json{SubPath}{fileName}", FileMode.Open, FileAccess.Read);
    }
}
