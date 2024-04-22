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

// ReSharper disable once CheckNamespace -- Must match the csharp_namespace in the DecimalValue.proto file
namespace Energinet.DataHub.Edi.Responses;

public partial class DecimalValue
{
    private const decimal NanoFactor = 1_000_000_000;

    public static DecimalValue FromDecimal(decimal d)
    {
        var units = decimal.ToInt64(d);
        var nanos = decimal.ToInt32((d - units) * NanoFactor);
        return new DecimalValue
        {
            Units = units,
            Nanos = nanos,
        };
    }

    public decimal ToDecimal() => Units + (Nanos / NanoFactor);
}
