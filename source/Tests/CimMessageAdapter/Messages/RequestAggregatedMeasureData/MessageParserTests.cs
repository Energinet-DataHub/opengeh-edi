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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Energinet.DataHub.EDI.B2CWebApi.Factories;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser;
using Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.AggregatedMeasureDataRequestMessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.CimXml;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Xunit;
using RequestAggregatedMeasureDataDto = Energinet.DataHub.EDI.IncomingMessages.Interfaces.RequestAggregatedMeasureDataDto;

namespace Energinet.DataHub.EDI.Tests.CimMessageAdapter.Messages.RequestAggregatedMeasureData;

public class MessageParserTests
{
    private const string SeriesDummy = "{\"mRID\":\"123353185\",\"balanceResponsibleParty_MarketParticipant.mRID\":{\"codingScheme\":\"A10\",\"value\":\"5799999933318\"},\"biddingZone_Domain.mRID\":{\"codingScheme\":\"A01\",\"value\":\"10YDK-1--------M\"},\"end_DateAndOrTime.dateTime\":\"2022-07-22T22:00:00Z\",\"energySupplier_MarketParticipant.mRID\":{\"codingScheme\":\"A10\",\"value\":\"5790001330552\"},\"marketEvaluationPoint.settlementMethod\":{\"value\":\"D01\"},\"marketEvaluationPoint.type\":{\"value\":\"E17\"},\"meteringGridArea_Domain.mRID\":{\"codingScheme\":\"NDK\",\"value\":\"244\"},\"settlement_Series.version\":{\"value\":\"D01\"},\"start_DateAndOrTime.dateTime\":\"2022-06-17T22:00:00Z\"},";

    private static readonly string PathToMessages =
        $"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}";

    private static readonly string SubPath =
        $"{Path.DirectorySeparatorChar}aggregatedmeasure{Path.DirectorySeparatorChar}";

    private readonly MarketMessageParser _marketMessageParser;

    public MessageParserTests()
    {
        using var logfac = new NullLoggerFactory();
        _marketMessageParser = new MarketMessageParser(
            new IMessageParser[]
            {
                new XmlMessageParser(new CimXmlSchemaProvider(new CimXmlSchemas(new Logger<CimXmlSchemas>(logfac)))),
                new JsonMessageParser(new JsonSchemaProvider(new CimJsonSchemas())),
                new B2CJsonMessageParser(new Serializer()),
            });
    }

    public static IEnumerable<object[]> CreateMessagesWithSingleAndMultipleTransactions()
    {
        return new List<object[]>
        {
            new object[] { DocumentFormat.Xml, CreateBaseXmlMessage("RequestAggregatedMeasureData.xml") },
            new object[] { DocumentFormat.Xml, CreateBaseXmlMessage("TwoSeriesRequestAggregatedMeasureData.xml") },
            new object[] { DocumentFormat.Json, CreateBaseJsonMessages("RequestAggregatedMeasureData.json") },
            new object[] { DocumentFormat.Json, CreateBaseJsonMessages("RequestAggregatedMeasureData.json", 1) },
        };
    }

    public static IEnumerable<object[]> CreateB2CMessagesWithSingleAndMultipleTransactions()
    {
        return new List<object[]>
        {
            new object[] { DocumentFormat.Json, CreateProtoMemoryStream() },
        };
    }

    public static IEnumerable<object[]> CreateBadMessages()
    {
        return new List<object[]>
        {
                new object[] { DocumentFormat.Xml, CreateBaseXmlMessage("BadVersionRequestAggregatedMeasureData.xml"), nameof(InvalidBusinessReasonOrVersion) },
                new object[] { DocumentFormat.Xml, CreateBaseXmlMessage("VersionIndexOutOfRangeRequestAggregatedMeasureData.xml"), nameof(InvalidMessageStructure) },
                new object[] { DocumentFormat.Json, CreateBaseJsonMessages("FailSchemeValidationAggregatedMeasureData.json"), nameof(InvalidMessageStructure) },
                new object[] { DocumentFormat.Json, CreateBaseJsonMessages("InvalidJsonAggregatedMeasureData.json"), nameof(InvalidMessageStructure) },
        };
    }

    [Theory]
    [MemberData(nameof(CreateMessagesWithSingleAndMultipleTransactions))]
    public async Task Successfully_parsed(DocumentFormat format, Stream message)
    {
        var result = await _marketMessageParser.ParseAsync(new IncomingMessageStream(message), format, IncomingDocumentType.RequestAggregatedMeasureData, CancellationToken.None);
        using var assertionScope = new AssertionScope();
        Assert.True(result.Success);
        var marketMessage = (RequestAggregatedMeasureDataMessage)result!.IncomingMessage!;
        Assert.NotNull(marketMessage);
        Assert.Equal("123564789123564789123564789123564789", marketMessage.MessageId);
        Assert.Equal("D05", marketMessage.BusinessReason);
        Assert.Equal("5799999933318", marketMessage.SenderNumber);
        Assert.Equal("DDK", marketMessage.SenderRoleCode);
        Assert.Equal("5790001330552", marketMessage.ReceiverNumber);
        Assert.Equal("DGL", marketMessage.ReceiverRoleCode);
        //Assert.Equal("2022-12-17T09:30:47Z", marketMessage.CreatedAt);
        Assert.Equal("23", marketMessage.BusinessType);

        foreach (var serie in marketMessage.Series.Cast<RequestAggregatedMeasureDataMessageSeries>())
        {
            Assert.NotNull(serie);
            Assert.Equal("123353185", serie.TransactionId);
            Assert.Equal("5799999933318", serie.BalanceResponsiblePartyId);
            Assert.Equal("5790001330552", serie.EnergySupplierId);
            Assert.Equal("E17", serie.MeteringPointType);
            Assert.Equal("244", serie.GridArea);
            Assert.Equal("D01", serie.SettlementMethod);
            Assert.Equal("2022-07-22T22:00:00Z", serie.EndDateTime);
            Assert.Equal("2022-06-17T22:00:00Z", serie.StartDateTime);
            Assert.Equal("D01", serie.SettlementVersion);
        }
    }

    [Theory]
    [MemberData(nameof(CreateB2CMessagesWithSingleAndMultipleTransactions))]
    public async Task Successfully_parsed_b2c_messages(DocumentFormat format, Stream message)
    {
        var result = await _marketMessageParser.ParseAsync(new IncomingMessageStream(message), format, IncomingDocumentType.B2CRequestAggregatedMeasureData, CancellationToken.None);
        using var assertionScope = new AssertionScope();
        Assert.True(result.Success);
        var marketMessage = (RequestAggregatedMeasureDataMessage)result!.IncomingMessage!;
        Assert.NotNull(marketMessage);
        Assert.Equal("123564789123564789123564789123564789", marketMessage.MessageId);
        Assert.Equal("D05", marketMessage.BusinessReason);
        Assert.Equal("5799999933318", marketMessage.SenderNumber);
        Assert.Equal("DDK", marketMessage.SenderRoleCode);
        Assert.Equal("5790001330552", marketMessage.ReceiverNumber);
        Assert.Equal("DGL", marketMessage.ReceiverRoleCode);
        //Assert.Equal("2022-12-17T09:30:47Z", marketMessage.CreatedAt);
        Assert.Equal("23", marketMessage.BusinessType);

        foreach (var serie in marketMessage.Series.Cast<RequestAggregatedMeasureDataMessageSeries>())
        {
            Assert.NotNull(serie);
            Assert.Equal("123353185", serie.TransactionId);
            Assert.Equal("5799999933318", serie.BalanceResponsiblePartyId);
            Assert.Equal("5790001330552", serie.EnergySupplierId);
            Assert.Equal("E17", serie.MeteringPointType);
            Assert.Equal("244", serie.GridArea);
            Assert.Equal("D01", serie.SettlementMethod);
            Assert.Equal("2022-07-22T22:00:00Z", serie.EndDateTime);
            Assert.Equal("2022-06-17T22:00:00Z", serie.StartDateTime);
            Assert.Equal("D01", serie.SettlementVersion);
        }
    }

    [Theory]
    [MemberData(nameof(CreateBadMessages))]
    public async Task Messages_with_errors(DocumentFormat format, Stream message, string expectedError)
    {
        var result = await _marketMessageParser.ParseAsync(new IncomingMessageStream(message), format, IncomingDocumentType.RequestAggregatedMeasureData, CancellationToken.None);

        Assert.NotEmpty(result.Errors);
        Assert.True(result.Success == false);
        Assert.True(expectedError != null);
        Assert.Contains(result.Errors, error => error.GetType().Name == expectedError);
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
        var serie = new RequestAggregatedMeasureDataSeries(
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
            SystemClock.Instance.GetCurrentInstant().ToString(),
            BusinessType: "23",
            new[] { serie });

        var jsonString = new Serializer().Serialize(request);
        var encoding = Encoding.UTF8;
        var byteArray = encoding.GetBytes(jsonString);
        var memoryStream = new MemoryStream(byteArray);
        return memoryStream;
    }
}
