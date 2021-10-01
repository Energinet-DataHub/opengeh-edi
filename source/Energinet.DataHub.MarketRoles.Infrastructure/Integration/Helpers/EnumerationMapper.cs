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
using System.Globalization;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace Energinet.DataHub.MarketRoles.Infrastructure.Integration.Helpers
{
    public static class EnumerationMapper
    {
        public static TEnum MapToEnum<TEnum>(this EnumerationType enumType)
            where TEnum : struct, Enum
        {
            if (enumType is null)
            {
                throw new ArgumentNullException(nameof(enumType));
            }

            if (Enum.TryParse<TEnum>(enumType.Id.ToString(CultureInfo.InvariantCulture), out var parsedEnum))
            {
                return parsedEnum;
            }

            throw new InvalidOperationException("Could not map EnumerationType to enum");
        }

        public static TEnumerationType MapToEnumerationType<TEnumerationType>(this Enum enumss)
            where TEnumerationType : EnumerationType
        {
            if (enumss is null)
            {
                throw new ArgumentNullException(nameof(enumss));
            }

            var intVal = Convert.ToInt32(enumss, CultureInfo.InvariantCulture);
            return EnumerationType.FromValue<TEnumerationType>(intVal);
        }
    }
}
