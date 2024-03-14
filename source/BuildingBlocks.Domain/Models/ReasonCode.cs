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

public class ReasonCode : EnumerationType
{
    public static readonly ReasonCode FullyAccepted = new(nameof(FullyAccepted), "A01");
    public static readonly ReasonCode FullyRejected = new(nameof(FullyRejected), "A02");

    public ReasonCode(string name, string code)
        : base(name)
    {
        Code = code;
    }

    public string Code { get; }

    public static ReasonCode From(string code)
    {
        return GetAll
                <ReasonCode>()
            .First(reasonCode => reasonCode.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    public static ReasonCode FromName(string name)
    {
        return GetAll<ReasonCode>()
            .First(reasonCode => reasonCode.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"{name} is not a valid {typeof(ReasonCode)} {nameof(name)}");
    }
}
