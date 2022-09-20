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
using System.Reflection;
using System.Text.Json.Serialization;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Infrastructure.Configuration;
using Xunit;

namespace Messaging.ArchitectureTests.Application;

public class InternalCommandTests
{
    [Theory(DisplayName = nameof(Has_json_constructor_attribute))]
    [MemberData(nameof(GetInternalCommands))]
    public void Has_json_constructor_attribute(Type internalCommand)
    {
        if (internalCommand == null) throw new ArgumentNullException(nameof(internalCommand));
        var jsonConstructorAttributes = internalCommand
            .GetConstructors()
            .SelectMany(c => c.GetCustomAttributes()
                .Where(t => t is JsonConstructorAttribute));

        Assert.True(jsonConstructorAttributes.Any());
    }

    private static IEnumerable<object[]> GetInternalCommands()
    {
        return ApplicationAssemblies.Application
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(InternalCommand)))
            .Select(t => new[] { t });
    }
}
