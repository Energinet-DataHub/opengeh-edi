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

using Xunit.Abstractions;
using Xunit.Sdk;

namespace Energinet.DataHub.EDI.SubsystemTests.TestOrdering;

public class TestOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : notnull, ITestCase
    {
        return testCases.OrderBy(GetTestMethodOrder).ToList();
    }

    private static int GetTestMethodOrder<TTestCase>(TTestCase testCase)
        where TTestCase : notnull, ITestCase
    {
        return testCase.TestMethod.Method
            .GetCustomAttributes(typeof(OrderAttribute).AssemblyQualifiedName!)
            .FirstOrDefault()
            ?.GetNamedArgument<int>(nameof(OrderAttribute.Number)) ?? 0;
    }
}
