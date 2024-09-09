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

using System.Reflection;
using System.Text.Json.Serialization;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications.Handlers;
using Energinet.DataHub.EDI.Process.Domain.Commands;
using Xunit;

namespace Energinet.DataHub.EDI.ArchitectureTests.Application;

public class InternalCommandTests
{
    public static IEnumerable<object[]> GetInternalCommands()
    {
        var allTypes = typeof(EnqueueAcceptedEnergyResultMessageHandler).Assembly.GetTypes();

        return allTypes
            .Where(t => t.IsSubclassOf(typeof(InternalCommand)))
            .Select(t => new[] { t });
    }

    [Theory(DisplayName = nameof(Has_json_constructor_attribute))]
    [MemberData(nameof(GetInternalCommands))]
    public void Has_json_constructor_attribute(Type internalCommand)
    {
        ArgumentNullException.ThrowIfNull(internalCommand);
        var jsonConstructorAttributes = internalCommand
            .GetConstructors()
            .SelectMany(c => c.GetCustomAttributes()
                .Where(t => t is JsonConstructorAttribute));

        Assert.True(jsonConstructorAttributes.Any());
    }
}
