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
using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace Energinet.DataHub.EDI.IntegrationTests;

public static class FluentAssertionExtensions
{
    /*
     * This RegEx test if a string looks like a CIM code
     * Here is an example: https://regex101.com/r/HhGUH4/1
     * The regex thinks the string is a CIM code if:
     * 1. The string MUST start with A, D, E, P, K, or H
     * 2. The string MUST be followed by 2 characters
     * 3. The string can be followed by 0 to 2 characters
     * So in total: The string start with A, D, E, P, K, or H has a total length of 3 to 5 characters
     * Match examples: D01, D32, E02, PT15M, P1D, P1M, KWH, H87, A09, E65, PT1H
     * Non-match examples: Fee, DanishCrowns, Exchange, abc, fee
     */
    private static readonly Regex _cimCodeRegex = new(@"^[A,D,E,P,K,H](..)(.?.?)$", RegexOptions.Compiled);

    [CustomAssertion]
    public static AndConstraint<StringAssertions> NotBeCimCode(this StringAssertions should)
    {
        ArgumentNullException.ThrowIfNull(should);

        return should.NotMatchRegex(_cimCodeRegex, "because value shouldn't look like a CIM code");
    }
}
