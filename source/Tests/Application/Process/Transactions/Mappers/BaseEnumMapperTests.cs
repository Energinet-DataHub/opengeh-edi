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

    protected static void EnsureCanMapOrThrows<TInputEnum>(Action performMapping, TInputEnum value, params TInputEnum[] invalidValues)
        where TInputEnum : Enum
    {
        // Act
        var act = performMapping;

        // Assert
        if (ValueIsValid(value, invalidValues))
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<InvalidOperationException>();
        }
    }

    protected static void EnsureCanMapOrReturnsNull<TEnumInput, TEnumResult>(Func<TEnumResult?> performMapping, TEnumInput value, params TEnumInput[] invalidValues)
        where TEnumInput : Enum
    {
        ArgumentNullException.ThrowIfNull(performMapping);

        // Act
        var act = performMapping;

        // Assert
        if (ValueIsValid(value, invalidValues))
        {
            var result = act.Should().NotThrow().Subject;
            result.Should().NotBeNull();
        }
        else
        {
            var result = act();
            result.Should().BeNull();
        }
    }

    private static bool ValueIsValid<TEnum>(TEnum value, params TEnum[] invalidValues)
        where TEnum : Enum
    {
        var valid = (int)(object)value != InvalidEnumNumber && !invalidValues.Contains(value);

        return valid;
    }
}
