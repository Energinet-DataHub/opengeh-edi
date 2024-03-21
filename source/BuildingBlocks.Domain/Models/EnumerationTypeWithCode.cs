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

public abstract class EnumerationTypeWithCode<T> : EnumerationType
    where T : EnumerationTypeWithCode<T>
{
    protected EnumerationTypeWithCode(string name, string code)
        : base(name)
    {
        Code = code;
    }

    public string Code { get; }

#pragma warning disable CA1000
    public static T FromName(string name)
    {
        return GetAll<T>().FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException(
                   $"{name} is not a valid {typeof(T).Name} {nameof(name)}");
    }

    public static T FromCode(string code)
    {
        return TryFromCode(code)
               ?? throw new InvalidOperationException(
                   $"{code} is not a valid {typeof(T).Name} {nameof(code)}");
    }

    public static T? TryFromCode(string code)
    {
        return GetAll<T>().FirstOrDefault(r => r.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    public static string? GetNameFromCode(string code)
    {
        return TryFromCode(code)?.Name ?? null;
    }
#pragma warning restore CA1000

    protected override bool ValueMatches(EnumerationType otherEnumerationType)
    {
        if (otherEnumerationType is not EnumerationTypeWithCode<T> otherEnumerationTypeWithCode)
            return false;

        return base.ValueMatches(otherEnumerationTypeWithCode) && Code.Equals(otherEnumerationTypeWithCode.Code, StringComparison.Ordinal);
    }
}
