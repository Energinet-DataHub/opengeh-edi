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

using System.Text.Json;
using AutoFixture.Xunit2;
using Energinet.DataHub.EDI.B2BApi.MeasurementsSynchronization;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Migration;

public class JsonFromXmlFieldExtractorTests
{
    [Theory]
    [InlineAutoData(
        XmlMessageConstants.PeekMessageContainingTwoTransactions,
        "83521745ef4f4ada83f2115dda402e30",
        "E17",
        "2023-12-31T23:00:00Z",
        "2024-01-16T08:54:58Z")]
    [InlineAutoData(
        XmlMessageConstants.PeekMessageContainingThreeTransactions,
        "EH_1889555899",
        "D14",
        "2024-05-25T22:00:00Z",
        "2024-05-30T05:36:59Z")]
    public void Given_XmlWithValidJson_When_ExtractJsonFromXmlCData_Then_ReturnsValidJson(
        string messageBody, string expectedOriginalTimeSeriesId, string expectedTypeOfMeteringPoint, string expectedStartTime, string expectedTransactionInsertDate)
    {
        // Arrange

        // Act
        var extractedJson = JsonFromXmlFieldExtractor.ExtractJsonFromXmlCData(messageBody);

        // Assert
        Assert.NotNull(extractedJson);
        Assert.NotEmpty(extractedJson);

        using var document = JsonDocument.Parse(extractedJson);
        var root = document.RootElement;

        var timeSeries = root.GetProperty("MeteredDataTimeSeriesDH3").GetProperty("TimeSeries");
        var originalTimeSeriesId = timeSeries[0].GetProperty("OriginalTimeSeriesId").GetString();
        var transactionInsertDate = timeSeries[0].GetProperty("TransactionInsertDate").GetString();
        var typeOfMp = timeSeries[0].GetProperty("TypeOfMP").GetString();
        var startTime = timeSeries[0].GetProperty("TimeSeriesPeriod").GetProperty("Start").GetString();

        originalTimeSeriesId.Should().Be(expectedOriginalTimeSeriesId);
        transactionInsertDate.Should().Be(expectedTransactionInsertDate);
        typeOfMp.Should().Be(expectedTypeOfMeteringPoint);
        startTime.Should().Be(expectedStartTime);
    }

    [Fact]
    public void Given_XmlWithOutJson_When_ExtractJsonFromXmlCData_Then_ThrowsInvalidOperationException()
    {
        // Arrange
        const string peekedMessageContent = XmlMessageConstants.PeekMessageWithoutPayload;
        const string expectedErrorMessage = "Could not parse '<ns0:CData>' from peeked message with reference: 7f071bd47baa493488aea08f41efcc08";

        // Act and Assert
        var exception = Assert.Throws<InvalidOperationException>(() => JsonFromXmlFieldExtractor.ExtractJsonFromXmlCData(peekedMessageContent));
        exception.Message.Should().Be(expectedErrorMessage);
    }
}
