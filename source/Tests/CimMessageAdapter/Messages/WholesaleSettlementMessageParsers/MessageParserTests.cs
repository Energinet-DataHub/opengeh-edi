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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser;
using Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.WholesaleSettlementMessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using FluentAssertions.Execution;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.CimMessageAdapter.Messages.WholesaleSettlementMessageParsers;

public class MessageParserTests
{
    private const string SeriesDummy = "{\"mRID\":\"25836143\",\"aggregationSeries_Period.resolution\":\"PT1M\",\"chargeTypeOwner_MarketParticipant.mRID\":{\"codingScheme\":\"A10\",\"value\":\"570001110111\"},\"end_DateAndOrTime.dateTime\": \"2022-08-31T22:00:00Z\",\"energySupplier_MarketParticipant.mRID\":{\"codingScheme\":\"A10\",\"value\":\"5799999933318\"},\"meteringGridArea_Domain.mRID\":{\"codingScheme\": \"NDK\",\"value\":\"244\"},\"settlement_Series.version\":{\"value\":\"D01\"},\"start_DateAndOrTime.dateTime\":\"2022-08-17T22:00:00Z\",\"ChargeType\":[{\"mRID\":\"EA-001\",\"type\":{\"value\":\"D03\"}}]},";

    private static readonly string PathToMessages =
        $"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}";

    private static readonly string SubPath =
        $"{Path.DirectorySeparatorChar}wholesalesettlement{Path.DirectorySeparatorChar}";

    private readonly MarketMessageParser _marketMessageParser;

    public MessageParserTests()
    {
        _marketMessageParser = new MarketMessageParser(
            new IMessageParser[]
            {
                new JsonMessageParser(new JsonSchemaProvider(new CimJsonSchemas())),
            });
    }

    public static IEnumerable<object[]> CreateMessagesWithSingleAndMultipleTransactions()
    {
        return new List<object[]>
        {
            new object[] { DocumentFormat.Json, CreateBaseJsonMessages("RequestWholesaleSettlement.json") },
            new object[] { DocumentFormat.Json, CreateBaseJsonMessages("RequestWholesaleSettlement.json", 1) },
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

    [Theory]
    [MemberData(nameof(CreateMessagesWithSingleAndMultipleTransactions))]
    public async Task Successfully_parsed(DocumentFormat format, Stream message)
    {
        var result = await _marketMessageParser.ParseAsync(new IncomingMessageStream(message), format, IncomingDocumentType.RequestWholesaleSettlement, CancellationToken.None);
        using var assertionScope = new AssertionScope();
        Assert.True(result.Success);
        var marketMessage = (RequestWholesaleServicesMessage)result!.IncomingMessage!;
        Assert.NotNull(marketMessage);
        Assert.Equal("12345678", marketMessage.MessageId);
        Assert.Equal("D05", marketMessage.BusinessReason);
        Assert.Equal("5799999933318", marketMessage.SenderNumber);
        Assert.Equal("DDQ", marketMessage.SenderRoleCode);
        Assert.Equal("5790001330552", marketMessage.ReceiverNumber);
        Assert.Equal("DDZ", marketMessage.ReceiverRoleCode);
        Assert.Equal("2022-12-17T09:30:47Z", marketMessage.CreatedAt);
        Assert.Equal("23", marketMessage.BusinessType);

        foreach (var serie in marketMessage.Serie.Cast<RequestWholesaleServiceSerie>())
        {
            Assert.NotNull(serie);
            Assert.Equal("25836143", serie.TransactionId);
            Assert.Equal("PT1M", serie.Resolution);
            Assert.Equal("570001110111", serie.ChargeOwner);
            Assert.Equal("5799999933318", serie.EnergySupplierMarketParticipantId);
            Assert.Equal("2022-08-31T22:00:00Z", serie.EndDateTime);
            Assert.Equal("244", serie.MeteringGridAreaDomainId);
            Assert.Equal("D01", serie.SettlementSeriesVersion);
            Assert.Equal("2022-08-17T22:00:00Z", serie.StartDateTime);
            foreach (var chargeType in serie.ChargeTypes)
            {
                Assert.True(chargeType != null);
                Assert.Equal("EA-001", chargeType.Id);
                Assert.Equal("D03", chargeType.Type);
            }
        }
    }

    [Theory]
    [MemberData(nameof(CreateBadMessages))]
    public async Task Messages_with_errors(DocumentFormat format, Stream message, string expectedError)
    {
        var result = await _marketMessageParser.ParseAsync(new IncomingMessageStream(message), format, IncomingDocumentType.RequestWholesaleSettlement, CancellationToken.None);

        Assert.NotEmpty(result.Errors);
        Assert.True(result.Success == false);
        Assert.True(expectedError != null);
        Assert.Contains(result.Errors, error => error.GetType().Name == expectedError);
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
}
