﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Infrastructure.Configuration;
using Energinet.DataHub.EDI.Infrastructure.DocumentValidation;
using Energinet.DataHub.Edi.Requests;
using FluentAssertions.Execution;
using Google.Protobuf;
using IncomingMessages.Infrastructure.Messages;
using IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData;
using IncomingMessages.Infrastructure.RequestAggregatedMeasureDataParsers;
using IncomingMessages.Infrastructure.ValidationErrors;
using Xunit;
using DocumentType = Energinet.DataHub.EDI.Common.DocumentType;

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
        _marketMessageParser = new MarketMessageParser(
            new IMessageParser[]
            {
                new XmlMessageParser(),
                new JsonMessageParser(new JsonSchemaProvider(new CimJsonSchemas())),
                new ProtoMessageParser(new SystemDateTimeProvider()),
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
            new object[] { DocumentFormat.Proto, CreateProtoMemoryStream() },
        };
    }

    public static IEnumerable<object[]> CreateBadMessages()
    {
        return new List<object[]>
        {
                new object[] { DocumentFormat.Xml, CreateBaseXmlMessage("BadVersionRequestAggregatedMeasureData.xml"), nameof(InvalidBusinessReasonOrVersion) },
                new object[] { DocumentFormat.Json, CreateBaseJsonMessages("FailSchemeValidationAggregatedMeasureData.json"), nameof(InvalidMessageStructure) },
        };
    }

    [Theory]
    [MemberData(nameof(CreateMessagesWithSingleAndMultipleTransactions))]
    public async Task Successfully_parsed(DocumentFormat format, Stream message)
    {
        var result = await _marketMessageParser.ParseAsync(message, format, IncomingDocumentType.RequestAggregatedMeasureData, CancellationToken.None);
        using var assertionScope = new AssertionScope();
        Assert.True(result.Success);
        var marketMessage = result!.MarketMessage!;
        Assert.True(marketMessage != null);
        Assert.Equal("123564789123564789123564789123564789", marketMessage.MessageId);
        Assert.Equal("D05", marketMessage.BusinessReason);
        Assert.Equal("5799999933318", marketMessage.SenderNumber);
        Assert.Equal("DDK", marketMessage.SenderRoleCode);
        Assert.Equal("5790001330552", marketMessage.ReceiverNumber);
        Assert.Equal("DGL", marketMessage.ReceiverRoleCode);
        //Assert.Equal("2022-12-17T09:30:47Z", marketMessage.CreatedAt);
        Assert.Equal("23", marketMessage.BusinessType);

        foreach (var serie in result!.MarketMessage!.Series)
        {
            Assert.True(serie != null);
            Assert.Equal("123353185", serie.Id);
            Assert.Equal("5799999933318", serie.BalanceResponsiblePartyMarketParticipantId);
            Assert.Equal("5790001330552", serie.EnergySupplierMarketParticipantId);
            Assert.Equal("E17", serie.MarketEvaluationPointType);
            Assert.Equal("244", serie.MeteringGridAreaDomainId);
            Assert.Equal("D01", serie.MarketEvaluationSettlementMethod);
            Assert.Equal("2022-07-22T22:00:00Z", serie.EndDateAndOrTimeDateTime);
            Assert.Equal("2022-06-17T22:00:00Z", serie.StartDateAndOrTimeDateTime);
            Assert.Equal("D01", serie.SettlementSeriesVersion);
        }
    }

    [Theory]
    [MemberData(nameof(CreateBadMessages))]
    public async Task Messages_with_errors(DocumentFormat format, Stream message, string expectedError)
    {
        var result = await _marketMessageParser.ParseAsync(message, format, IncomingDocumentType.RequestAggregatedMeasureData, CancellationToken.None);

        Assert.True(result.Errors.Count > 0);
        Assert.True(result.Success == false);
        Assert.True(expectedError != null);
        Assert.Contains(result.Errors, error => error.GetType().Name == expectedError);
    }

    private static Stream CreateBaseXmlMessage(string fileName)
    {
        var xmlDocument = XDocument.Load(
            $"{PathToMessages}xml{SubPath}{fileName}");

        var stream = new MemoryStream();
        xmlDocument.Save(stream);

        return stream;
    }

    private static Stream CreateBaseJsonMessages(string fileName, int addSeriesUntilMbSize = 0)
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
        var protostuff = new Edi.Requests.RequestAggregatedMeasureData
        {
            MessageId = "123564789123564789123564789123564789",
            MessageType = "E74",
            BusinessReason = "D05",
            SenderRoleCode = "DDK",
            SenderId = "5799999933318",
            ReceiverId = "5790001330552",
            ReceiverRoleCode = "DGL",
        };
        protostuff.Series.Add(new Serie
        {
            Id = "123353185",
            SettlementSeriesVersion = "D01",
            MarketEvaluationPointType = "E17",
            MeteringGridAreaDomainId = "244",
            MarketEvaluationSettlementMethod = "D01",
            EnergySupplierMarketParticipantId = "5790001330552",
            BalanceResponsiblePartyMarketParticipantId = "5799999933318",
            StartDateAndOrTimeDateTime = "2022-06-17T22:00:00Z",
            EndDateAndOrTimeDateTime = "2022-07-22T22:00:00Z",
        });
        var bytes = protostuff.ToByteArray();
        var memoryStream = new MemoryStream();
        memoryStream.Write(bytes, 0, bytes.Length);
        memoryStream.Position = 0;
        return memoryStream;
    }
}
