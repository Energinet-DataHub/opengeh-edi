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
using Energinet.DataHub.Wholesale.Contracts.Events;
using Xunit;

namespace Tests.Domain.Transactions.Aggregations;

public class CalculationResultCompletedEventMapperTests
{
    public static IEnumerable<object[]> ProcessTypes()
    {
        foreach (var number in Enum.GetValues(typeof(ProcessType)))
        {
            yield return new[] { number };
        }
    }

    [Theory]
    [MemberData(nameof(ProcessTypes))]
    public void Ensure_handling_all_process_types(ProcessType processType)
    {
        // Act
        if (processType != ProcessType.Unspecified)
        {
            CalculationResultCompletedEventMapperSpy.MapProcessTypeSpy(processType);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => CalculationResultCompletedEventMapperSpy.MapProcessTypeSpy(processType));
        }
    }
}
