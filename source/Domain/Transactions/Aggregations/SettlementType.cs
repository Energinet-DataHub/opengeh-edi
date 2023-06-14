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

using Domain.SeedWork;

namespace Domain.Transactions.Aggregations;

public class SettlementType : EnumerationType
{
    // customer with more than ~100.000 kwH
    public static readonly SettlementType NonProfiled = new(0, nameof(NonProfiled), "E02");

    // customer with less than ~100.000 kwH
    public static readonly SettlementType Flex = new(1, nameof(Flex), "XXX");

    private SettlementType(int id, string name, string code)
        : base(id, name)
    {
        Code = code;
    }

    public string Code { get; }

    public static SettlementType From(string valueToParse)
    {
        var settlementType = GetAll<SettlementType>()
            .FirstOrDefault(type => type.Name.Equals(valueToParse, StringComparison.OrdinalIgnoreCase) ||
                                    type.Code.Equals(valueToParse, StringComparison.OrdinalIgnoreCase));

        if (settlementType is null)
            throw new InvalidCastException($"Could not parse {valueToParse} to settlement type");

        return settlementType;
    }
}
