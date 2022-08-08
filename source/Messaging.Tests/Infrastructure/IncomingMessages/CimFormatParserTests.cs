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
using Messaging.Domain.SeedWork;
using Xunit;

namespace Messaging.Tests.Infrastructure.IncomingMessages;

public class CimFormatParserTests
{
    [Theory]
    [InlineData("application/json", nameof(CimFormat.Json))]
    [InlineData("application/json; charset=utf-8", nameof(CimFormat.Json))]
    public void Can_parse_from_content_header_value(string contentHeaderValue, string expectedCimFormat)
    {
        var expectedFormat = EnumerationType.FromName<CimFormat>(expectedCimFormat);
        var parsedFormat = CimFormat.ParseFromContentHeaderValue(contentHeaderValue);

        Assert.Equal(expectedFormat, parsedFormat);
    }
}

#pragma warning disable

public class CimFormat : EnumerationType
{
    public static readonly CimFormat Xml = new CimFormat(0, nameof(Xml));
    public static readonly CimFormat Json = new CimFormat(1, nameof(Json));
    public static readonly CimFormat Unknown = new CimFormat(100, nameof(Unknown));
    private CimFormat(int id, string name)
        : base(id, name)
    {
    }

    public static CimFormat ParseFromContentHeaderValue(string value)
    {
        var contentTypeValues = value.Split(";");
        if (contentTypeValues[0].Equals("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return Json;
        }

        return Unknown;
    }
}
