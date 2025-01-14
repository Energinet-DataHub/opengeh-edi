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

using System.Text.Encodings.Web;
using System.Text.Unicode;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM009;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;
using Reason = Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM009.Reason;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM009;

public class AcknowledgementJsonDocumentWriterTests
{
    private readonly AcknowledgementJsonDocumentWriter _sut = new AcknowledgementJsonDocumentWriter(
        new MessageRecordParser(new Serializer()),
        JavaScriptEncoder.Create(
            UnicodeRanges.BasicLatin,
            UnicodeRanges.Latin1Supplement,
            UnicodeRanges.LatinExtendedA));

    [Fact]
    public async Task METHOD()
    {
        var reasons = new Reason[] { new("A22", "Some error occured"), new("A23", "Some other error occured"), };
        var timePeriods = new TimePeriod[]
        {
            new(
                new Interval(
                    SystemClock.Instance.GetCurrentInstant(),
                    SystemClock.Instance.GetCurrentInstant()),
                reasons),
            new(
                new Interval(
                    SystemClock.Instance.GetCurrentInstant(),
                    SystemClock.Instance.GetCurrentInstant()),
                reasons),
        };

        var acknowledgementRecord = new AcknowledgementRecord(
            TransactionId.New(),
            SystemClock.Instance.GetCurrentInstant(),
            TransactionId.New(),
            "A10",
            "2",
            "DocumentTitle",
            "ERR",
            reasons,
            timePeriods,
            [new Series("bbf014de7733", reasons), new Series("aa0484c10e6d", reasons)],
            [new MktActivityRecord("680048962dd2", reasons), new MktActivityRecord("b455eba88025", reasons)],
            [new TimeSeries("123", "1", timePeriods, reasons), new TimeSeries("456", "2", timePeriods, reasons)]);

        var marketDocumentStream = await _sut.WriteAsync(
            new OutgoingMessageHeader(
                "A22",
                "2222222222222",
                "DDM",
                "1111111111111",
                "DDQ",
                "MessageId",
                SystemClock.Instance.GetCurrentInstant()),
            new Serializer().Serialize(acknowledgementRecord));

        marketDocumentStream.Stream.Position = 0;
        var streamReader = new StreamReader(marketDocumentStream.Stream);
        var json = await streamReader.ReadToEndAsync();

        using var assertionScope = new AssertionScope();
        var jsonSchema = await NJsonSchema.JsonSchema.FromFileAsync(
            @"Infrastructure\OutgoingMessages\RSM009\Acknowledgement-assembly-model.schema.json");
        jsonSchema.Validate(json).Should().BeEmpty();

        json.Should().NotBeEmpty();
    }
}
