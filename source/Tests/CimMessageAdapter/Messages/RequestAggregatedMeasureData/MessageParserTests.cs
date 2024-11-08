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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.AggregatedMeasureDataRequestMessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Json;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;
using RequestAggregatedMeasureDataDto = Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models.RequestAggregatedMeasureDataDto;

namespace Energinet.DataHub.EDI.Tests.CimMessageAdapter.Messages.RequestAggregatedMeasureData;

public sealed class MessageParserTests
{
    private const string SeriesDummy = "{\"mRID\":\"123353185\",\"balanceResponsibleParty_MarketParticipant.mRID\":{\"codingScheme\":\"A10\",\"value\":\"5799999933318\"},\"biddingZone_Domain.mRID\":{\"codingScheme\":\"A01\",\"value\":\"10YDK-1--------M\"},\"end_DateAndOrTime.dateTime\":\"2022-07-22T22:00:00Z\",\"energySupplier_MarketParticipant.mRID\":{\"codingScheme\":\"A10\",\"value\":\"5790001330552\"},\"marketEvaluationPoint.settlementMethod\":{\"value\":\"D01\"},\"marketEvaluationPoint.type\":{\"value\":\"E17\"},\"meteringGridArea_Domain.mRID\":{\"codingScheme\":\"NDK\",\"value\":\"244\"},\"settlement_Series.version\":{\"value\":\"D01\"},\"start_DateAndOrTime.dateTime\":\"2022-06-17T22:00:00Z\"},";

    private static readonly string PathToMessages =
        $"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}";

    private static readonly string SubPath =
        $"{Path.DirectorySeparatorChar}aggregatedmeasure{Path.DirectorySeparatorChar}";

    private readonly MarketMessageParser _marketMessageParser = new(
        [
            new AggregatedMeasureDataXmlMessageParser(new CimXmlSchemaProvider(new CimXmlSchemas())),
            new AggregatedMeasureDataJsonMessageParser(new JsonSchemaProvider(new CimJsonSchemas())),
            new AggregatedMeasureDataB2CJsonMessageParser(new Serializer()),
        ]);

    public static IEnumerable<object[]> CreateMessagesWithSingleAndMultipleTransactions()
    {
        return
        [
            [DocumentFormat.Xml, CreateBaseXmlMessage("RequestAggregatedMeasureData.xml")],
            [DocumentFormat.Xml, CreateBaseXmlMessage("TwoSeriesRequestAggregatedMeasureData.xml")],
            [DocumentFormat.Json, CreateBaseJsonMessages("RequestAggregatedMeasureData.json")],
            [DocumentFormat.Json, CreateBaseJsonMessages("RequestAggregatedMeasureData.json", 1)],
        ];
    }

    public static IEnumerable<object[]> CreateB2CMessagesWithSingleAndMultipleTransactions()
    {
        return
        [
            [DocumentFormat.Json, CreateProtoMemoryStream()],
        ];
    }

    public static IEnumerable<object[]> CreateBadMessages()
    {
        return
        [
            [
                DocumentFormat.Xml, CreateBaseXmlMessage("BadVersionRequestAggregatedMeasureData.xml"),
                nameof(InvalidBusinessReasonOrVersion),
            ],
            [
                DocumentFormat.Xml, CreateBaseXmlMessage("VersionIndexOutOfRangeRequestAggregatedMeasureData.xml"),
                nameof(InvalidMessageStructure),
            ],
            [
                DocumentFormat.Json, CreateBaseJsonMessages("FailSchemeValidationAggregatedMeasureData.json"),
                nameof(InvalidMessageStructure),
            ],
            [
                DocumentFormat.Json, CreateBaseJsonMessages("InvalidJsonAggregatedMeasureData.json"),
                nameof(InvalidMessageStructure),
            ],
            [
                DocumentFormat.Json, CreateBaseJsonMessages("EmptyJsonObject.json"),
                nameof(InvalidMessageStructure),
            ],
        ];
    }

    [Theory]
    [MemberData(nameof(CreateMessagesWithSingleAndMultipleTransactions))]
    public async Task Successfully_parsed(DocumentFormat format, Stream message)
    {
        var result = await _marketMessageParser.ParseAsync(
            new IncomingMarketMessageStream(message),
            format,
            IncomingDocumentType.RequestAggregatedMeasureData,
            CancellationToken.None);

        using var assertionScope = new AssertionScope();
        result.Success.Should().BeTrue();

        var marketMessage = (RequestAggregatedMeasureDataMessage)result.IncomingMessage!;
        marketMessage.Should().NotBeNull();
        marketMessage.MessageId.Should().Be("123564789123564789123564789123564789");
        marketMessage.BusinessReason.Should().Be("D05");
        marketMessage.SenderNumber.Should().Be("5799999933318");
        marketMessage.SenderRoleCode.Should().Be("DDK");
        marketMessage.ReceiverNumber.Should().Be("5790001330552");
        marketMessage.ReceiverRoleCode.Should().Be("DGL");
        marketMessage.CreatedAt.Should().Be("2022-12-17T09:30:47Z");
        marketMessage.BusinessType.Should().Be("23");

        foreach (var series in marketMessage.Series.Cast<RequestAggregatedMeasureDataMessageSeries>())
        {
            series.Should().NotBeNull();
            series.TransactionId.Should().Be("123353185");
            series.BalanceResponsiblePartyId.Should().Be("5799999933318");
            series.EnergySupplierId.Should().Be("5790001330552");
            series.MeteringPointType.Should().Be("E17");
            series.GridArea.Should().Be("244");
            series.SettlementMethod.Should().Be("D01");
            series.EndDateTime.Should().Be("2022-07-22T22:00:00Z");
            series.StartDateTime.Should().Be("2022-06-17T22:00:00Z");
            series.SettlementVersion.Should().Be("D01");
        }
    }

    [Theory]
    [MemberData(nameof(CreateB2CMessagesWithSingleAndMultipleTransactions))]
    public async Task Successfully_parsed_b2c_messages(DocumentFormat format, Stream message)
    {
        var result = await _marketMessageParser.ParseAsync(
            new IncomingMarketMessageStream(message),
            format,
            IncomingDocumentType.B2CRequestAggregatedMeasureData,
            CancellationToken.None);

        using var assertionScope = new AssertionScope();
        result.Success.Should().BeTrue();

        var marketMessage = (RequestAggregatedMeasureDataMessage)result.IncomingMessage!;
        marketMessage.Should().NotBeNull();
        marketMessage.MessageId.Should().Be("123564789123564789123564789123564789");
        marketMessage.BusinessReason.Should().Be("D05");
        marketMessage.SenderNumber.Should().Be("5799999933318");
        marketMessage.SenderRoleCode.Should().Be("DDK");
        marketMessage.ReceiverNumber.Should().Be("5790001330552");
        marketMessage.ReceiverRoleCode.Should().Be("DGL");
        marketMessage.CreatedAt.Should().Be("2022-12-17T09:30:47Z");
        marketMessage.BusinessType.Should().Be("23");

        foreach (var series in marketMessage.Series.Cast<RequestAggregatedMeasureDataMessageSeries>())
        {
            series.Should().NotBeNull();
            series.TransactionId.Should().Be("123353185");
            series.BalanceResponsiblePartyId.Should().Be("5799999933318");
            series.EnergySupplierId.Should().Be("5790001330552");
            series.MeteringPointType.Should().Be("E17");
            series.GridArea.Should().Be("244");
            series.SettlementMethod.Should().Be("D01");
            series.EndDateTime.Should().Be("2022-07-22T22:00:00Z");
            series.StartDateTime.Should().Be("2022-06-17T22:00:00Z");
            series.SettlementVersion.Should().Be("D01");
        }
    }

    [Theory]
    [MemberData(nameof(CreateBadMessages))]
    public async Task Messages_with_errors(DocumentFormat format, Stream message, string expectedError)
    {
        var result = await _marketMessageParser.ParseAsync(new IncomingMarketMessageStream(message), format, IncomingDocumentType.RequestAggregatedMeasureData, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        expectedError.Should().NotBeNull();
        result.Errors.Should().Contain(error => error.GetType().Name == expectedError);
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

    private static MemoryStream CreateProtoMemoryStream()
    {
        var dateTimeZone = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];
        var series = new RequestAggregatedMeasureDataSeries(
            "123353185",
            "E17",
            "D01",
            InstantFormatFactory.SetInstantToMidnight("2022-06-17T22:00:00Z", dateTimeZone).ToString(),
            InstantFormatFactory.SetInstantToMidnight("2022-07-22T22:00:00Z", dateTimeZone).ToString(),
            "244",
            "5790001330552",
            "5799999933318",
            "D01");

        var request = new RequestAggregatedMeasureDataDto(
            "5799999933318",
            "DDK",
            DataHubDetails.DataHubActorNumber.Value,
            ActorRole.MeteredDataAdministrator.Code,
            "D05",
            "E74",
            "123564789123564789123564789123564789",
            "2022-12-17T09:30:47Z",
            BusinessType: "23",
            [series]);

        var jsonString = new Serializer().Serialize(request);
        var encoding = Encoding.UTF8;
        var byteArray = encoding.GetBytes(jsonString);
        var memoryStream = new MemoryStream(byteArray);

        return memoryStream;
    }
}
