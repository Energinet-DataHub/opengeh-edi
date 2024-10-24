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

using System.Reflection;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using FluentAssertions;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.MessageParsers;

public class GivenNewIncomingDocumentTypeTests : IncomingMessagesTestBase
{
    private static readonly List<(IncomingDocumentType, DocumentFormat)> _unsupportedCombinationsOfIncomingDocumentTypeAndDocumentFormat = new()
    {
        (IncomingDocumentType.B2CRequestWholesaleSettlement, DocumentFormat.Xml),
        (IncomingDocumentType.B2CRequestAggregatedMeasureData, DocumentFormat.Xml),
        (IncomingDocumentType.RequestWholesaleSettlement, DocumentFormat.Ebix),
        (IncomingDocumentType.RequestAggregatedMeasureData, DocumentFormat.Ebix),
        (IncomingDocumentType.B2CRequestWholesaleSettlement, DocumentFormat.Ebix),
        (IncomingDocumentType.B2CRequestAggregatedMeasureData, DocumentFormat.Ebix),
        // TODO: Remove when implementing parsers for CIM
        (IncomingDocumentType.MeteredDataForMeasurementPoint, DocumentFormat.Xml),
        (IncomingDocumentType.MeteredDataForMeasurementPoint, DocumentFormat.Json),
    };

    public GivenNewIncomingDocumentTypeTests(IncomingMessagesTestFixture incomingMessagesTestFixture, ITestOutputHelper testOutputHelper)
        : base(incomingMessagesTestFixture, testOutputHelper)
    {
    }

    public static TheoryData<IncomingDocumentType, DocumentFormat> GetAllIncomingDocumentTypeAndDocumentFormats
    {
        get
        {
            var data = new TheoryData<IncomingDocumentType, DocumentFormat>();

            var incomingDocumentTypes =
                typeof(IncomingDocumentType).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            var documentFormats =
                typeof(DocumentFormat).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

            foreach (var documentType in incomingDocumentTypes)
            {
                foreach (var documentFormat in documentFormats)
                {
                    data.Add((IncomingDocumentType)documentType.GetValue(null)!, (DocumentFormat)documentFormat.GetValue(null)!);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(GetAllIncomingDocumentTypeAndDocumentFormats))]
    public async Task When_ParsingMessageOfDocumentTypeAndFormat_Then_ExpectedMessageParserFound(
        IncomingDocumentType incomingDocumentType,
        DocumentFormat documentFormat)
    {
        // Arrange
        var marketMessageParser = GetService<MarketMessageParser>();

        // Act
        var act = () => marketMessageParser.ParseAsync(
            new IncomingMarketMessageStream(new MemoryStream()),
            documentFormat,
            incomingDocumentType,
            CancellationToken.None);

        // Assert
        if (_unsupportedCombinationsOfIncomingDocumentTypeAndDocumentFormat.Contains((incomingDocumentType, documentFormat)))
        {
            await act.Should().ThrowAsync<InvalidOperationException>("because this combination is not supported");
        }
        else
        {
            await act.Should().NotThrowAsync<InvalidOperationException>("because this combination is valid, but no parser was found");
        }
    }
}
