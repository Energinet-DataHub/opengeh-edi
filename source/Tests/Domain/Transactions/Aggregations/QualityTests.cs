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

using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Domain.Transactions.Aggregations;

public class QualityTests
{
    [Theory]
    [InlineData("missing", "A02")]
    [InlineData("estimated", "A03")]
    [InlineData("measured", "A04")]
    [InlineData("incomplete", "A05")]
    [InlineData("calculated", "A06")]
    [InlineData("a02", "A02")]
    [InlineData("a03", "A03")]
    [InlineData("a04", "A04")]
    [InlineData("a05", "A05")]
    [InlineData("a06", "A06")]
    public void Can_parse(string valueToParseFrom, string expectedCode)
    {
        var quality = Quality.From(valueToParseFrom);

        Assert.Equal(expectedCode, quality.Code);
    }
}
