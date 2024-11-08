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

using System.Text;
using System.Xml.Linq;
using Energinet.DataHub.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.B2CWebApi.Factories;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.WholesaleSettlementMessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Json;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NodaTime.Extensions;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.CimMessageAdapter.Messages.WholesaleSettlementMessageParsers;

public sealed class MessageParserTests
{
    private const string SeriesDummy =
        "{\"mRID\":\"25836143\",\"aggregationSeries_Period.resolution\":\"P1M\",\"chargeTypeOwner_MarketParticipant.mRID\":{\"codingScheme\":\"A10\",\"value\":\"570001110111\"},\"end_DateAndOrTime.dateTime\": \"2022-08-31T22:00:00Z\",\"energySupplier_MarketParticipant.mRID\":{\"codingScheme\":\"A10\",\"value\":\"5799999933318\"},\"meteringGridArea_Domain.mRID\":{\"codingScheme\": \"NDK\",\"value\":\"244\"},\"settlement_Series.version\":{\"value\":\"D01\"},\"start_DateAndOrTime.dateTime\":\"2022-08-17T22:00:00Z\",\"ChargeType\":[{\"mRID\":\"EA-001\",\"type\":{\"value\":\"D03\"}}]},";

    private static readonly string PathToMessages =
        $"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}";

    private static readonly string SubPath =
        $"{Path.DirectorySeparatorChar}wholesalesettlement{Path.DirectorySeparatorChar}";

    private readonly MarketMessageParser _marketMessageParser;
    private readonly Serializer _serializer = new();

    public MessageParserTests()
    {
        _marketMessageParser = new MarketMessageParser(
        [
            new WholesaleSettlementXmlMessageParser(new CimXmlSchemaProvider(new CimXmlSchemas())),
                new WholesaleSettlementJsonMessageParser(new JsonSchemaProvider(new CimJsonSchemas())),
                new WholesaleSettlementB2CJsonMessageParser(_serializer),
        ]);
    }

    public static IEnumerable<object[]> CreateMessagesWithTwoChargeTypes()
    {
        return
        [
            [DocumentFormat.Json, CreateBaseJsonMessages("RequestWholesaleSettlement2ChargeTypes.json")],
            [DocumentFormat.Xml, CreateBaseXmlMessage("RequestWholesaleSettlement2ChargeTypes.xml")],
        ];
    }

    public static IEnumerable<object[]> CreateMessagesWithSingleAndMultipleTransactions()
    {
        return
        [
            [DocumentFormat.Json, CreateBaseJsonMessages("RequestWholesaleSettlement.json")],
            [DocumentFormat.Json, CreateBaseJsonMessages("RequestWholesaleSettlement.json", 1)],
            [DocumentFormat.Xml, CreateBaseXmlMessage("RequestWholesaleSettlement.xml")],
            [DocumentFormat.Xml, CreateBaseXmlMessage("RequestWholesaleSettlementTwoSeries.xml")],
        ];
    }

    public static IEnumerable<object[]> CreateBadMessages()
    {
        return
        [
            [
                DocumentFormat.Json, CreateBaseJsonMessages("FailSchemeValidationRequestWholesaleSettlement.json"),
                nameof(InvalidMessageStructure),
            ],
            [
                DocumentFormat.Json, CreateBaseJsonMessages("InvalidJsonRequestWholesaleSettlement.json"),
                nameof(InvalidMessageStructure),
            ],
            [
                DocumentFormat.Json, CreateBaseJsonMessages("EmptyJsonObject.json"),
                nameof(InvalidMessageStructure),
            ],
        ];
    }

    public static IEnumerable<object[]> CreateMessagesWithOneBigSeriesAndOneSmall()
    {
        return
        [
            [DocumentFormat.Xml, CreateBaseXmlMessage("RequestWholesaleSettlementOneSmallOneBigSeries.xml")],
            [DocumentFormat.Json, CreateBaseJsonMessages("RequestWholesaleSettlementOneSmallOneBigSeries.json")],
        ];
    }

    [Theory]
    [MemberData(nameof(CreateMessagesWithSingleAndMultipleTransactions))]
    public async Task Successfully_parsed(DocumentFormat format, Stream message)
    {
        var result = await _marketMessageParser.ParseAsync(new IncomingMarketMessageStream(message), format, IncomingDocumentType.RequestWholesaleSettlement, CancellationToken.None);
        using var assertionScope = new AssertionScope();
        result.Success.Should().BeTrue();

        var marketMessage = (RequestWholesaleServicesMessage)result.IncomingMessage!;
        marketMessage.Should().NotBeNull();
        marketMessage.MessageId.Should().Be("12345678");
        marketMessage.BusinessReason.Should().Be("D05");
        marketMessage.SenderNumber.Should().Be("5799999933318");
        marketMessage.SenderRoleCode.Should().Be("DDQ");
        marketMessage.ReceiverNumber.Should().Be("5790001330552");
        marketMessage.ReceiverRoleCode.Should().Be("DDZ");
        marketMessage.CreatedAt.Should().Be("2022-12-17T09:30:47Z");
        marketMessage.BusinessType.Should().Be("23");

        foreach (var series in marketMessage.Series.Cast<RequestWholesaleServicesSeries>())
        {
            series.Should().NotBeNull();
            series.TransactionId.Should().Be("25836143");
            series.ChargeOwner.Should().Be("570001110111");
            series.EnergySupplierId.Should().Be("5799999933318");
            series.EndDateTime.Should().Be("2022-08-31T22:00:00Z");
            series.GridArea.Should().Be("244");
            series.SettlementVersion.Should().Be("D01");
            series.StartDateTime.Should().Be("2022-08-17T22:00:00Z");
            series.Resolution.Should().Be("P1M");

            var chargeType = series.ChargeTypes.Should().ContainSingle().Subject;
            chargeType.Id.Should().Be("EA-001");
            chargeType.Type.Should().Be("D03");
        }
    }

    [Theory]
    [MemberData(nameof(CreateMessagesWithTwoChargeTypes))]
    public async Task Given_MessageWithTwoChargeTypes_When_Parsing_Then_SuccessfullyParses(
        DocumentFormat format,
        Stream message)
    {
        var result = await _marketMessageParser.ParseAsync(
            new IncomingMarketMessageStream(message),
            format,
            IncomingDocumentType.RequestWholesaleSettlement,
            CancellationToken.None);

        using var assertionScope = new AssertionScope();

        result.Success.Should().BeTrue();

        var marketMessage = (RequestWholesaleServicesMessage)result.IncomingMessage!;
        marketMessage.Should().NotBeNull();

        var series = marketMessage.Series.Cast<RequestWholesaleServicesSeries>().ToList();

        var firstChargeType = series.First().ChargeTypes.First();
        var secondChargeType = series.First().ChargeTypes.Last();

        firstChargeType.Id.Should().Be("EA-001");
        firstChargeType.Type.Should().Be("D03");

        secondChargeType.Id.Should().Be("EA-002");
        secondChargeType.Type.Should().Be("D02");
    }

    [Theory]
    [MemberData(nameof(CreateBadMessages))]
    public async Task Messages_with_errors(DocumentFormat format, Stream message, string expectedError)
    {
        var result = await _marketMessageParser.ParseAsync(new IncomingMarketMessageStream(message), format, IncomingDocumentType.RequestWholesaleSettlement, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        expectedError.Should().NotBeNull();
        result.Errors.Should().Contain(error => error.GetType().Name == expectedError);
    }

    [Theory]
    [MemberData(nameof(CreateMessagesWithOneBigSeriesAndOneSmall))]
    public async Task Given_MessageWithTwoSeries_When_Parsing_Then_SuccessfullyParses(
        DocumentFormat format,
        Stream message)
    {
        var result = await _marketMessageParser.ParseAsync(
            new IncomingMarketMessageStream(message),
            format,
            IncomingDocumentType.RequestWholesaleSettlement,
            CancellationToken.None);

        using var assertionScope = new AssertionScope();

        result.Success.Should().BeTrue();

        var marketMessage = (RequestWholesaleServicesMessage)result.IncomingMessage!;
        marketMessage.Should().NotBeNull();

        var series = marketMessage.Series.Cast<RequestWholesaleServicesSeries>().ToList();

        series.Should().HaveCount(2);

        var bigSeries = series.First();
        var smallSeries = series.Last();

        bigSeries.TransactionId.Should().Be("111111111111");
        bigSeries.Resolution.Should().Be("PT1M");
        bigSeries.ChargeOwner.Should().Be("570001110111");
        bigSeries.EnergySupplierId.Should().Be("5799999933318");
        bigSeries.EndDateTime.Should().Be("2022-08-31T22:00:00Z");
        bigSeries.GridArea.Should().Be("244");
        bigSeries.SettlementVersion.Should().Be("D01");
        bigSeries.StartDateTime.Should().Be("2022-08-17T22:00:00Z");
        bigSeries.ChargeTypes.Should().HaveCount(3);

        var chargeTypes = bigSeries.ChargeTypes.ToList();

        for (var i = 0; i < chargeTypes.Count; i++)
        {
            chargeTypes[i].Id.Should().Be($"EA-00{i + 1}");
            chargeTypes[i].Type.Should().Be($"D0{i + 1}");
        }

        smallSeries.TransactionId.Should().Be("1");
        smallSeries.StartDateTime.Should().Be("2022-08-17T22:00:00Z");
        smallSeries.Resolution.Should().BeNull();
        smallSeries.ChargeOwner.Should().BeNull();
        smallSeries.EnergySupplierId.Should().BeNull();
        smallSeries.EndDateTime.Should().BeNull();
        smallSeries.GridArea.Should().BeNull();
        smallSeries.SettlementVersion.Should().BeNull();
        smallSeries.ChargeTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_B2CRequest_When_Parsing_Then_SuccessfullyParsed()
    {
        // Arrange
        var b2CRequest = new RequestWholesaleSettlementMarketRequest(
            CalculationType.BalanceFixing,
            "2022-08-17T22:00:00Z",
            "2022-08-31T22:00:00Z",
            "804",
            "5799999933318",
            null,
            PriceType.MonthlySubscription);

        var message = RequestWholesaleSettlementDtoFactory.Create(
            b2CRequest,
            "5799999933318",
            "EnergySupplier",
            DateTimeZoneProviders.Tzdb.GetSystemDefault(),
            DateTime.Now.ToUniversalTime().ToInstant());

        // Act
        var result = await _marketMessageParser.ParseAsync(
            GenerateStreamFromString(
                _serializer.Serialize(message)),
            DocumentFormat.Json,
            IncomingDocumentType.B2CRequestWholesaleSettlement,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    private static MemoryStream CreateBaseXmlMessage(string fileName)
    {
        var xmlDocument = XDocument.Load(
            $"{PathToMessages}xml{SubPath}{fileName}");

        var stream = new MemoryStream();
        xmlDocument.Save(stream);

        return stream;
    }

    private static MemoryStream CreateBaseJsonMessages(string fileName, int addSeriesUntilMbSize = 0)
    {
        return ReadTextFile($"{PathToMessages}json{SubPath}{fileName}", addSeriesUntilMbSize);
    }

    private static MemoryStream ReadTextFile(string path, int addSeriesUntilMbSize = 0)
    {
        var jsonDoc = File.ReadAllText(path);
        if (addSeriesUntilMbSize > 0)
        {
            jsonDoc = ChangeJsonFileSizeTo(jsonDoc, addSeriesUntilMbSize);
        }

        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream: stream, encoding: Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
        writer.Write(jsonDoc);
        writer.Flush();
        stream.Position = 0;

        return stream;
    }

    private static string ChangeJsonFileSizeTo(string jsonDoc, int addSeriesUntilMbSize)
    {
        var newFileSizeInBytes = addSeriesUntilMbSize * 1024 * 1024;
        var indexOfSeries = jsonDoc.IndexOf("Series", StringComparison.Ordinal);
        if (indexOfSeries < 0)
        {
            return jsonDoc;
        }

        var seriesStartAtIndex = indexOfSeries + "Series".Length + 4;
        var jsonDocSb = new StringBuilder(jsonDoc);

        while (jsonDocSb.Length < newFileSizeInBytes)
        {
            jsonDocSb.Insert(seriesStartAtIndex, SeriesDummy, 10);
        }

        return jsonDocSb.ToString();
    }

    private static IncomingMarketMessageStream GenerateStreamFromString(string jsonString)
    {
        var encoding = Encoding.UTF8;
        var byteArray = encoding.GetBytes(jsonString);
        var memoryStream = new MemoryStream(byteArray);
        return new IncomingMarketMessageStream(memoryStream);
    }
}
