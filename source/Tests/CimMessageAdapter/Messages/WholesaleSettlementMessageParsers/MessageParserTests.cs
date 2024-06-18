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
using Energinet.DataHub.EDI.B2CWebApi.Factories;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.WholesaleSettlementMessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Json;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using RequestWholesaleSettlementChargeType = Energinet.DataHub.EDI.B2CWebApi.Models.RequestWholesaleSettlementChargeType;

namespace Energinet.DataHub.EDI.Tests.CimMessageAdapter.Messages.WholesaleSettlementMessageParsers;

public class MessageParserTests
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
            new IMarketMessageParser[]
            {
                new WholesaleSettlementXmlMessageParser(new CimXmlSchemaProvider(new CimXmlSchemas())),
                new WholesaleSettlementJsonMessageParser(new JsonSchemaProvider(new CimJsonSchemas())),
                new WholesaleSettlementB2CJsonMessageParser(_serializer),
            });
    }

    public static IEnumerable<object[]> CreateMessagesWithTwoChargeTypes()
    {
        return new List<object[]>
        {
            new object[] { DocumentFormat.Json, CreateBaseJsonMessages("RequestWholesaleSettlement2ChargeTypes.json") },
            new object[] { DocumentFormat.Xml, CreateBaseXmlMessage("RequestWholesaleSettlement2ChargeTypes.xml") },
        };
    }

    public static IEnumerable<object[]> CreateMessagesWithSingleAndMultipleTransactions()
    {
        return new List<object[]>
        {
            new object[] { DocumentFormat.Json, CreateBaseJsonMessages("RequestWholesaleSettlement.json") },
            new object[] { DocumentFormat.Json, CreateBaseJsonMessages("RequestWholesaleSettlement.json", 1) },
            new object[] { DocumentFormat.Xml, CreateBaseXmlMessage("RequestWholesaleSettlement.xml") },
            new object[] { DocumentFormat.Xml, CreateBaseXmlMessage("RequestWholesaleSettlementTwoSeries.xml") },
        };
    }

    public static IEnumerable<object[]> CreateBadMessages()
    {
        return new List<object[]>
        {
                new object[] { DocumentFormat.Json, CreateBaseJsonMessages("FailSchemeValidationRequestWholesaleSettlement.json"), nameof(InvalidMessageStructure) },
                new object[] { DocumentFormat.Json, CreateBaseJsonMessages("InvalidJsonRequestWholesaleSettlement.json"), nameof(InvalidMessageStructure) },
        };
    }

    public static IEnumerable<object[]> CreateMessagesWithOneBigSeriesAndOneSmall()
    {
        return new List<object[]>
        {
            new object[] { DocumentFormat.Xml, CreateBaseXmlMessage("RequestWholesaleSettlementOneSmallOneBigSeries.xml"), },
            new object[] { DocumentFormat.Json, CreateBaseJsonMessages("RequestWholesaleSettlementOneSmallOneBigSeries.json") },
        };
    }

    [Theory]
    [MemberData(nameof(CreateMessagesWithSingleAndMultipleTransactions))]
    public async Task Successfully_parsed(DocumentFormat format, Stream message)
    {
        var result = await _marketMessageParser.ParseAsync(new IncomingMarketMessageStream(message), format, IncomingDocumentType.RequestWholesaleSettlement, CancellationToken.None);
        using var assertionScope = new AssertionScope();
        Assert.True(result.Success);
        var marketMessage = (RequestWholesaleServicesMessage)result.IncomingMessage!;
        Assert.NotNull(marketMessage);
        Assert.Equal("12345678", marketMessage.MessageId);
        Assert.Equal("D05", marketMessage.BusinessReason);
        Assert.Equal("5799999933318", marketMessage.SenderNumber);
        Assert.Equal("DDQ", marketMessage.SenderRoleCode);
        Assert.Equal("5790001330552", marketMessage.ReceiverNumber);
        Assert.Equal("DDZ", marketMessage.ReceiverRoleCode);
        Assert.Equal("2022-12-17T09:30:47Z", marketMessage.CreatedAt);
        Assert.Equal("23", marketMessage.BusinessType);

        foreach (var serie in marketMessage.Series.Cast<RequestWholesaleServicesSeries>())
        {
            Assert.NotNull(serie);
            Assert.Equal("25836143", serie.TransactionId);
            Assert.Equal("570001110111", serie.ChargeOwner);
            Assert.Equal("5799999933318", serie.EnergySupplierId);
            Assert.Equal("2022-08-31T22:00:00Z", serie.EndDateTime);
            Assert.Equal("244", serie.GridArea);
            Assert.Equal("D01", serie.SettlementVersion);
            Assert.Equal("2022-08-17T22:00:00Z", serie.StartDateTime);
            Assert.Single(serie.ChargeTypes);
            Assert.Equal("EA-001", serie.ChargeTypes.First().Id);
            Assert.Equal("D03", serie.ChargeTypes.First().Type);
            Assert.Equal("P1M", serie.Resolution);
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

        Assert.True(result.Success);

        var marketMessage = (RequestWholesaleServicesMessage)result.IncomingMessage!;
        Assert.NotNull(marketMessage);

        var series = marketMessage.Series.Cast<RequestWholesaleServicesSeries>().ToList();

        var firstChargeType = series.First().ChargeTypes.First();
        var secondChargeType = series.First().ChargeTypes.Last();

        Assert.Equal("EA-001", firstChargeType.Id);
        Assert.Equal("D03", firstChargeType.Type);

        Assert.Equal("EA-002", secondChargeType.Id);
        Assert.Equal("D02", secondChargeType.Type);
    }

    [Theory]
    [MemberData(nameof(CreateBadMessages))]
    public async Task Messages_with_errors(DocumentFormat format, Stream message, string expectedError)
    {
        var result = await _marketMessageParser.ParseAsync(new IncomingMarketMessageStream(message), format, IncomingDocumentType.RequestWholesaleSettlement, CancellationToken.None);

        Assert.NotEmpty(result.Errors);
        Assert.True(result.Success == false);
        Assert.True(expectedError != null);
        Assert.Contains(result.Errors, error => error.GetType().Name == expectedError);
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

        Assert.True(result.Success);

        var marketMessage = (RequestWholesaleServicesMessage)result.IncomingMessage!;
        Assert.NotNull(marketMessage);

        var series = marketMessage.Series.Cast<RequestWholesaleServicesSeries>().ToList();

        Assert.Equal(2, series.Count);

        var bigSeries = series.First();
        var smallSeries = series.Last();

        Assert.Equal("111111111111", bigSeries.TransactionId);
        Assert.Equal("PT1M", bigSeries.Resolution);
        Assert.Equal("570001110111", bigSeries.ChargeOwner);
        Assert.Equal("5799999933318", bigSeries.EnergySupplierId);
        Assert.Equal("2022-08-31T22:00:00Z", bigSeries.EndDateTime);
        Assert.Equal("244", bigSeries.GridArea);
        Assert.Equal("D01", bigSeries.SettlementVersion);
        Assert.Equal("2022-08-17T22:00:00Z", bigSeries.StartDateTime);
        Assert.Equal(3, bigSeries.ChargeTypes.Count);

        var chargeTypes = bigSeries.ChargeTypes.ToList();

        for (var i = 0; i < chargeTypes.Count; i++)
        {
            Assert.Equal($"EA-00{i + 1}", chargeTypes[i].Id);
            Assert.Equal($"D0{i + 1}", chargeTypes[i].Type);
        }

        Assert.Equal("1", smallSeries.TransactionId);
        Assert.Equal("2022-08-17T22:00:00Z", smallSeries.StartDateTime);
        Assert.Null(smallSeries.Resolution);
        Assert.Null(smallSeries.ChargeOwner);
        Assert.Null(smallSeries.EnergySupplierId);
        Assert.Null(smallSeries.EndDateTime);
        Assert.Null(smallSeries.GridArea);
        Assert.Null(smallSeries.SettlementVersion);
        Assert.Empty(smallSeries.ChargeTypes);
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
            "PT1H",
            "570001110111",
            new List<RequestWholesaleSettlementChargeType>
            {
                new("EA-001", "D03"),
                new("EA-002", "D02"),
            });
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
