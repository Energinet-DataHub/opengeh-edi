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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;

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

    protected static void EnsureCanMapOrReturnsNull<TEnumInput, TEnumResult>(
        Func<TEnumResult?> performMapping,
        TEnumInput value,
        TEnumInput unspecifiedValue,
        TEnumInput? notSupportedValue = default,
        params TEnumInput[] invalidValues)
        where TEnumInput : Enum
    {
        ArgumentNullException.ThrowIfNull(performMapping);

        // Act
        var act = performMapping;

        // Assert
        if (ValueIsValid(value, unspecifiedValue, notSupportedValue, invalidValues))
        {
            var result = act.Should().NotThrow().Subject;
            result.Should().NotBeNull();
        }
        else if (invalidValues.Contains(value))
        {
            var result = act();
            result.Should().BeNull();
        }
        else if (notSupportedValue is not null && value.Equals(notSupportedValue))
        {
            act.Should().Throw<NotSupportedException>();
        }
        else if (value.Equals(unspecifiedValue))
        {
            act.Should().Throw<InvalidOperationException>();
        }
        else
        {
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    protected static void EnsureCanMapOrReturnsNull<TEnumInput, TEnumResult>(
        Func<TEnumResult?> performMapping,
        TEnumInput value,
        params TEnumInput[] invalidValues)
        where TEnumInput : Enum
    {
        ArgumentNullException.ThrowIfNull(performMapping);

        // Act
        var act = performMapping;

        // Assert
        if (ValueIsValid(value, invalidValues))
        {
            act.Should().NotThrow();
        }
        else if (invalidValues.Contains(value))
        {
            var result = act();
            result.Should().BeNull();
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

    private static bool ValueIsValid<TEnum>(TEnum value, params TEnum[] invalidValues)
        where TEnum : Enum
    {
        var valid = (int)(object)value != InvalidEnumNumber && !invalidValues.Contains(value);

        return valid;
    }
}
