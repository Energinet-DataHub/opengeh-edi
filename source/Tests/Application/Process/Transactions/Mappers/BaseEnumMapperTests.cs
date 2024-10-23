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

using FluentAssertions;

namespace Energinet.DataHub.EDI.Tests.Application.Process.Transactions.Mappers;

public abstract class BaseEnumMapperTests
{
    protected const int InvalidEnumNumber = -326;

    public static IEnumerable<object[]> GetEnumValues(Type? @enum)
    {
        ArgumentNullException.ThrowIfNull(@enum);

        foreach (var enumValue in Enum.GetValues(@enum))
        {
            yield return new[] { enumValue };
        }

        yield return new object[] { InvalidEnumNumber }; // Test with invalid enum value
    }

    protected static void EnsureCanMapOrThrows<TEnum>(
        Action performMapping,
        TEnum value,
        TEnum unspecifiedValue,
        params TEnum[] invalidValues)
        where TEnum : Enum
    {
        // Act
        var act = performMapping;

        // Assert
        if (ValueIsValid(value, unspecifiedValue, default, invalidValues))
        {
            act.Should().NotThrow();
        }
        else if (invalidValues.Contains(value) || value.Equals(unspecifiedValue))
        {
            act.Should().Throw<InvalidOperationException>();
        }
        else
        {
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    private static bool ValueIsValid<TEnum>(TEnum value, TEnum? unspecifiedValue, TEnum? notSupportedValue, params TEnum[] invalidValues)
        where TEnum : Enum
    {
        var valid = (int)(object)value != InvalidEnumNumber && !value.Equals(unspecifiedValue) && !value.Equals(notSupportedValue) && !invalidValues.Contains(value);

        return valid;
    }
}
