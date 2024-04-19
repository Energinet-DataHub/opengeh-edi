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

using System.Globalization;
using FluentAssertions;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;

public static class StringConversionExtensions
{
    public static decimal ToDecimal(this string value)
    {
        var success = decimal.TryParse(value, CultureInfo.InvariantCulture, out var converted);
        success.Should().BeTrue("because {0} should be a valid decimal", value);
        return converted;
    }

    public static int ToInt(this string value)
    {
        var success = int.TryParse(value, CultureInfo.InvariantCulture, out var converted);
        success.Should().BeTrue("because {0} should be a valid int", value);
        return converted;
    }
}
