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
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using Google.Protobuf;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages;

public sealed class JsonEncoderTests
{
    private const string TestString = "ÅØÆÄÖåøæäö";

    [Fact]
    public void DefaultEncoder_DoesNotHandleScandinavianCharacters()
    {
        // Arrange
        var options = new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.Default };
        var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, options);

        // Act
        writer.WriteStringValue(TestString);
        writer.Flush();
        var result = Encoding.UTF8.GetString(stream.ToArray());

        // Assert
        result.Should().NotContain(TestString);
    }

    [Fact]
    public void CustomEncoder_HandlesScandinavianCharacters()
    {
        // Arrange
        var options = new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.Create(
                UnicodeRanges.BasicLatin,
                UnicodeRanges.Latin1Supplement,
                UnicodeRanges.LatinExtendedA),
        };
        var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, options);

        // Act
        writer.WriteStringValue(TestString);
        writer.Flush();
        var result = Encoding.UTF8.GetString(stream.ToArray());

        // Assert
        result.Should().Contain(TestString);
    }

    [Fact]
    public void JsonDocumentParse_HandlesScandinavianCharacters()
    {
        const string jsonPropertyName = "møøseStræng";

        // Arrange
        var options = new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.Default };
        var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, options);

        // Act
        writer.WriteStartObject();
        writer.WritePropertyName(jsonPropertyName);
        writer.WriteStringValue(TestString);
        writer.WriteEndObject();
        writer.Flush();

        // Assert
        Encoding.UTF8.GetString(stream.ToArray())
            .Should()
            .Contain(
                """
                {
                  "m\u00F8\u00F8seStr\u00E6ng": "\u00C5\u00D8\u00C6\u00C4\u00D6\u00E5\u00F8\u00E6\u00E4\u00F6"
                }
                """);

        var jsonDocument = JsonDocument.Parse(stream.ToArray());
        jsonDocument.Should().NotBeNull();

        var jsonElement = jsonDocument.RootElement.GetProperty(jsonPropertyName);
        jsonElement.Should().NotBeNull();

        jsonElement.GetString().Should().Be(TestString);
    }

    [Fact]
    public void ProtoBuff_HandlesScandinavianCharacters()
    {
        // Arrange
        var message = new WholesaleServicesRequestRejected();
        message.RejectReasons.Add(new RejectReason { ErrorCode = "mØøSe", ErrorMessage = TestString });

        // Act
        byte[] serializedMessage;
        using (var memoryStream = new MemoryStream())
        {
            using (var codedOutputStream = new CodedOutputStream(memoryStream))
            {
                message.WriteTo(codedOutputStream);
                codedOutputStream.Flush();
                serializedMessage = memoryStream.ToArray();
            }
        }

        var deserializedMessage = WholesaleServicesRequestRejected.Parser.ParseFrom(serializedMessage);

        // Assert
        deserializedMessage.RejectReasons.Should().ContainSingle();

        var rejectReason = deserializedMessage.RejectReasons.Single();
        rejectReason.ErrorCode.Should().Be("mØøSe");
        rejectReason.ErrorMessage.Should().Be(TestString);
    }

    [Fact]
    public void DataHubSerializer_HandlesScandinavianCharacters()
    {
        // Arrange
        var serializer = new Serializer();

        // Act
        var serialized = serializer.Serialize(TestString);

        // Assert
        serialized.Should().Contain(TestString);
    }
}
