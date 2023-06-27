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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Application.IncomingMessages.RequestAggregatedMeasureData;
using CimMessageAdapter.Messages;
using CimMessageAdapter.Messages.RequestAggregatedMeasureData;
using CimMessageAdapter.ValidationErrors;
using DocumentValidation;
using Infrastructure.IncomingMessages.RequestAggregatedMeasureData;
using Xunit;
using DocumentFormat = Domain.Documents.DocumentFormat;

namespace Tests.CimMessageAdapter.Messages.RequestAggregatedMeasureData;

public class MessageParserTests
{
    private const string SeriesDummy = "{\"mRID\":\"123353185\",\"balanceResponsibleParty_MarketParticipant.mRID\":{\"codingScheme\":\"A10\",\"value\":\"5799999933318\"},\"biddingZone_Domain.mRID\":{\"codingScheme\":\"A01\",\"value\":\"10YDK-1--------M\"},\"end_DateAndOrTime.dateTime\":\"2022-07-22T22:00:00Z\",\"energySupplier_MarketParticipant.mRID\":{\"codingScheme\":\"A10\",\"value\":\"5790001330552\"},\"marketEvaluationPoint.settlementMethod\":{\"value\":\"D01\"},\"marketEvaluationPoint.type\":{\"value\":\"E17\"},\"meteringGridArea_Domain.mRID\":{\"codingScheme\":\"NDK\",\"value\":\"244\"},\"settlement_Series.version\":{\"value\":\"D01\"},\"start_DateAndOrTime.dateTime\":\"2022-06-17T22:00:00Z\"},";

    private static readonly string PathToMessages =
        $"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}";

    private static readonly string SubPath =
        $"{Path.DirectorySeparatorChar}aggregatedmeasure{Path.DirectorySeparatorChar}";

    private readonly MessageParser _messageParser;

    public MessageParserTests()
    {
        _messageParser = new MessageParser(
            new IMessageParser<Serie, RequestAggregatedMeasureDataTransaction>[]
            {
                new XmlMessageParser(),
                new JsonMessageParser(new JsonSchemaProvider(new CimJsonSchemas())),
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

    public static IEnumerable<object[]> CreateBadMessages()
    {
        return new List<object[]>
        {
                new object[] { DocumentFormat.Xml, CreateBaseXmlMessage("BadVersionRequestAggregatedMeasureData.xml"), nameof(InvalidBusinessReasonOrVersion) },
                new object[] { DocumentFormat.Xml, CreateBaseXmlMessage("RequestAggregatedMeasureData.xml", 51), nameof(MessageSizeExceeded) },
                new object[] { DocumentFormat.Json, CreateBaseJsonMessages("FailSchemeValidationAggregatedMeasureData.json"), nameof(InvalidMessageStructure) },
                new object[] { DocumentFormat.Json, CreateBaseJsonMessages("RequestAggregatedMeasureData.json", 51), nameof(MessageSizeExceeded) },
        };
    }

    [Theory]
    [MemberData(nameof(CreateMessagesWithSingleAndMultipleTransactions))]
    public async Task Successfully_parsed(DocumentFormat format, Stream message)
    {
        var result = await _messageParser.ParseAsync(message, format, CancellationToken.None).ConfigureAwait(false);

        Assert.True(result.Success);
        var messageHeader = result!.IncomingMarketDocument!.Header;
        Assert.True(messageHeader != null);
        Assert.Equal("123564789123564789123564789123564789", messageHeader.MessageId);
        Assert.Equal("D05", messageHeader.BusinessReason);
        Assert.Equal("5799999933318", messageHeader.SenderId);
        Assert.Equal("DDK", messageHeader.SenderRole);
        Assert.Equal("5790001330552", messageHeader.ReceiverId);
        Assert.Equal("DGL", messageHeader.ReceiverRole);
        Assert.Equal("2022-12-17T09:30:47Z", messageHeader.CreatedAt);

        foreach (var serie in result!.IncomingMarketDocument!.MarketActivityRecords)
        {
            Assert.True(serie != null);
            Assert.Equal("123353185", serie.Id);
            Assert.Equal("5799999933318", serie.BalanceResponsiblePartyMarketParticipantId);
            Assert.Equal("10YDK-1--------M", serie.BiddingZoneDomainId);
            Assert.Equal("5790001330552", serie.EnergySupplierMarketParticipantId);
            Assert.Equal("E17", serie.MarketEvaluationPointType);
            Assert.Equal("244", serie.MeteringGridAreaDomainId);
            Assert.Equal("D01", serie.MarketEvaluationSettlementMethod);
            Assert.Equal("D01", serie.SettlementSeriesVersion);
            Assert.Equal("2022-07-22T22:00:00Z", serie.EndDateAndOrTimeDateTime);
            Assert.Equal("2022-06-17T22:00:00Z", serie.StartDateAndOrTimeDateTime);
        }
    }

    [Theory]
    [MemberData(nameof(CreateBadMessages))]
    public async Task Messages_with_errors(DocumentFormat format, Stream message, string expectedError)
    {
        var result = await _messageParser.ParseAsync(message, format, CancellationToken.None).ConfigureAwait(false);

        Assert.True(result.Errors.Count > 0);
        Assert.True(result.Success == false);
        Assert.True(expectedError != null);
        Assert.Contains(result.Errors, error => error.GetType().Name == expectedError);
    }

    #region xml messages
    private static Stream CreateBaseXmlMessage(string fileName, int newFileSizeInMb = 0)
    {
        var xmlDocument = XDocument.Load(
            $"{PathToMessages}xml{SubPath}{fileName}");
        if (newFileSizeInMb > 0)
        {
            return ChangeXmlFileSizeTo(xmlDocument, newFileSizeInMb);
        }

        var stream = new MemoryStream();
        xmlDocument.Save(stream);

        return stream;
    }

    private static Stream ChangeXmlFileSizeTo(XDocument document, int newFileSizeInMb)
    {
        var newFileSizeInBytes = newFileSizeInMb * 1024 * 1024;
        var message = new MemoryStream();
        document.Save(message, SaveOptions.DisableFormatting);
        message.Position = 0;
        if (message.Length > newFileSizeInBytes) return message;

        var remainSize = newFileSizeInBytes - message.Length;

        byte[] data = new byte[remainSize];
        message.Write(data);

        message.Position = 0;
        return message;
    }

    #endregion

    #region json messages
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
        int indexOfSeries = jsonDoc.IndexOf("Series", StringComparison.Ordinal);
        if (indexOfSeries < 0)
        {
            return jsonDoc;
        }

        int seriesStartAtIndex = indexOfSeries + "Series".Length + 4;
        var jsonDocSb = new StringBuilder(jsonDoc);

        while (jsonDocSb.Length < newFileSizeInBytes)
        {
            jsonDocSb.Insert(seriesStartAtIndex, SeriesDummy, 10);
        }

        return jsonDocSb.ToString();
    }

    #endregion
}
