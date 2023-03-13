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

using Domain.Transactions.Aggregations;
using Xunit;

namespace Tests.Domain.Transactions.Aggregations;

public class ResolutionTests
{
    [Theory]
    [InlineData("pt1h")]
    [InlineData("hourly")]
    public void Can_parse_from_name_or_code(string valueToParseFrom)
    {
        var resolution = Resolution.From(valueToParseFrom);

        Assert.Equal(Resolution.Hourly, resolution);
    }
}
