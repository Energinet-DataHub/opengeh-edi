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
using FluentAssertions;
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
}
