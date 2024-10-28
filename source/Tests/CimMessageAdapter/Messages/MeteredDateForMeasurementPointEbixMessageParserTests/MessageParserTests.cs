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
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.MeteredDateForMeasurementPointParsers.Ebix;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Ebix;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.CimMessageAdapter.Messages.MeteredDateForMeasurementPointEbixMessageParserTests;

public sealed class MessageParserTests
{
    private static readonly string PathToMessages =
        $"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}";

    private static readonly string SubPath =
        $"{Path.DirectorySeparatorChar}MeteredDateForMeasurementPoint{Path.DirectorySeparatorChar}";

    private readonly MarketMessageParser _marketMessageParser = new(
    [
        new MeteredDateForMeasurementPointEbixMessageParser(new EbixSchemaProvider(), new Logger<MeteredDateForMeasurementPointEbixMessageParser>(new LoggerFactory())),
    ]);

    public static TheoryData<DocumentFormat, Stream> CreateMessagesWithSingleAndMultipleTransactions()
    {
        var data = new TheoryData<DocumentFormat, Stream>
        {
            { DocumentFormat.Ebix, CreateBaseEbixMessage("ValidMeteredDateForMeasurementPoint.xml") },
            { DocumentFormat.Ebix, CreateBaseEbixMessage("ValidMeteredDateForMeasurementPointWithTwoTransactions.xml") },
            { DocumentFormat.Ebix, CreateBaseEbixMessage("ValidPT1HMeteredDataForMeasurementPoint.xml") },
        };

        return data;
    }

    public static TheoryData<DocumentFormat, Stream, string> CreateBadMessages()
    {
        var data = new TheoryData<DocumentFormat, Stream, string>
        {
            { DocumentFormat.Ebix, CreateBaseEbixMessage("BadVersionMeteredDateForMeasurementPoint.xml"), nameof(InvalidBusinessReasonOrVersion) },
            { DocumentFormat.Ebix, CreateBaseEbixMessage("InvalidMeteredDateForMeasurementPoint.xml"), nameof(InvalidMessageStructure) },
        };

        return data;
    }

    [Theory]
    [MemberData(nameof(CreateMessagesWithSingleAndMultipleTransactions))]
    public async Task Successfully_parsed(DocumentFormat format, Stream message)
    {
        var result = await _marketMessageParser.ParseAsync(
            new IncomingMarketMessageStream(message),
            format,
            IncomingDocumentType.MeteredDataForMeasurementPoint,
            CancellationToken.None);

        using var assertionScope = new AssertionScope();
        result.Success.Should().BeTrue();

        var marketMessage = (MeteredDataForMeasurementPointMessage)result.IncomingMessage!;
        marketMessage.Should().NotBeNull();
        marketMessage.MessageId.Should().Be("111131835");
        marketMessage.MessageType.Should().Be("E66");
        marketMessage.CreatedAt.Should().Be("2024-07-30T07:30:54Z");
        marketMessage.SenderNumber.Should().Be("5790001330552");
        marketMessage.ReceiverNumber.Should().Be("5790000432752");
        marketMessage.SenderRoleCode.Should().Be("MDR");
        marketMessage.BusinessReason.Should().Be("E23");
        marketMessage.ReceiverRoleCode.Should().BeEmpty();
        marketMessage.BusinessType.Should().Be("23");

        foreach (var series in marketMessage.Series.Cast<MeteredDataForMeasurementPointSeries>())
        {
            series.Should().NotBeNull();
            series.TransactionId.Should()
                .Match(transactionId => transactionId == "4413675032_5080574373" || transactionId == "4413675032_5080574374");
            series.Resolution.Should()
                .Match(resolution => resolution == "PT15M" || resolution == "PT1H");
            series.StartDateTime.Should()
                .Match(startDate => startDate == "2024-06-28T22:00:00Z" || startDate == "2024-06-29T22:00:00Z");
            series.EndDateTime.Should()
                .Match(endDate => endDate == "2024-06-29T22:00:00Z" || endDate == "2024-06-30T22:00:00Z");
            series.ProductNumber.Should().Be("8716867000030");
            series.ProductUnitType.Should().Be("KWH");
            series.MeteringPointType.Should().Be("E18");
            series.MeteringPointLocationId.Should().Be("571313000000002000");
            series.GridArea.Should().BeNull();

            var position = 1;
            foreach (var energyObservation in series.EnergyObservations)
            {
                energyObservation.Should().NotBeNull();
                energyObservation.Position.Should().Be(position.ToString());
                energyObservation.EnergyQuantity.Should().NotBeEmpty();
                energyObservation.QuantityQuality.Should()
                    .Match(quality => quality == "E01" || quality == "56");
                position++;
            }
        }
    }

    [Theory]
    [MemberData(nameof(CreateBadMessages))]
    public async Task Messages_with_errors(DocumentFormat format, Stream message, string expectedError)
    {
        var result = await _marketMessageParser.ParseAsync(
            new IncomingMarketMessageStream(message),
            format,
            IncomingDocumentType.MeteredDataForMeasurementPoint,
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        expectedError.Should().NotBeNull();
        result.Errors.Should().Contain(error => error.GetType().Name == expectedError);
    }

    private static MemoryStream CreateBaseEbixMessage(string fileName)
    {
        var xmlDocument = XDocument.Load(
            $"{PathToMessages}ebix{SubPath}{fileName}");

        var stream = new MemoryStream();
        xmlDocument.Save(stream);

        return stream;
    }
}
