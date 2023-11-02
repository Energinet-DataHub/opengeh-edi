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
using Energinet.DataHub.EDI.Common;

namespace Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations;

public class Quality : EnumerationType
{
    public static readonly Quality Missing = new(0, nameof(Missing), "A02");
    public static readonly Quality Estimated = new(1, nameof(Estimated), "A03");
    public static readonly Quality Measured = new(2, nameof(Measured), "A04");
    public static readonly Quality Incomplete = new(3, nameof(Incomplete), "A05");
    public static readonly Quality Calculated = new(4, nameof(Calculated), "A06");

    public Quality(int id, string name, string code)
        : base(id, name)
    {
        Code = code;
    }

    public string Code { get; }

    public static Quality From(string valueToParseFrom)
    {
        return GetAll
                <Quality>()
            .First(quality => quality.Name.Equals(valueToParseFrom, StringComparison.OrdinalIgnoreCase) ||
                              quality.Code.Equals(valueToParseFrom, StringComparison.OrdinalIgnoreCase));
    }
}
