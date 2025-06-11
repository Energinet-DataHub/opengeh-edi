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
using Energinet.DataHub.EDI.B2BApi.Migration;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM012;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Ebix;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Migration;

public class MeasurementsJsonToEbixStreamWriterTests
{
    [Fact]
    public async Task Given_ValidJson_When_WriteStreamAsync_Then_ReturnsValidMarketDocumentStream()
    {
        // Arrange
        var serializer = new Serializer();
        var timeSeriesJsonToEbixStreamWriter = new MeasurementsJsonToEbixStreamWriter(
            serializer, new List<IDocumentWriter> { new MeteredDataForMeteringPointEbixDocumentWriter(new MessageRecordParser(serializer)) });
        var meteredDataForMeteringPointEbixMessageParser = new MeteredDataForMeteringPointEbixMessageParser(
            new EbixSchemaProvider(),
            new Logger<MeteredDataForMeteringPointEbixMessageParser>(new LoggerFactory()));

        // Act
        var timeSeriesPayload = Encoding.UTF8.GetBytes(JsonPayloadConstants.SingleTimeSeriesWithSingleObservation);
        var stream = await timeSeriesJsonToEbixStreamWriter.WriteStreamAsync(new BinaryData(timeSeriesPayload));

        // Assert
        var res = await meteredDataForMeteringPointEbixMessageParser.ParseAsync(new IncomingMarketMessageStream(stream), CancellationToken.None);

        using var assertionScope = new AssertionScope();
        res.Errors.Should().BeEmpty();
        res.Success.Should().BeTrue();
        res.IncomingMessage.Should().NotBeNull();
    }
}
