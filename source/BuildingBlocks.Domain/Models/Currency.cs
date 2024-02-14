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
using System.Linq;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

public class Currency : EnumerationType
{
    public static readonly Currency DanishCrowns = new(nameof(DanishCrowns), "DKK");

    private Currency(string name, string code)
        : base(name)
    {
        Code = code;
    }

    public string Code { get; }

    public static Currency From(string value)
    {
        return GetAll<Currency>().First(currency =>
            currency.Code.Equals(value, StringComparison.OrdinalIgnoreCase) ||
            currency.Name.Equals(value, StringComparison.OrdinalIgnoreCase));
    }
}
