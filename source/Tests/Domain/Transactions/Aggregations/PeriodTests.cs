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
using NodaTime.Text;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Domain.Transactions.Aggregations;

public class PeriodTests
{
    [Fact]
    public void Adhere_to_format_rules()
    {
        var start = InstantPattern.General.Parse("2022-02-12T23:00:10Z").Value;
        var end = InstantPattern.General.Parse("2022-02-13T23:00:10Z").Value;

        var period = new Period(start, end);

        Assert.Equal("2022-02-12T23:00Z", period.StartToString());
        Assert.Equal("2022-02-13T23:00Z", period.EndToString());
    }
}
