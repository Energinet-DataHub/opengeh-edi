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
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Infrastructure.Configuration.InternalCommands;

public class InternalCommandRegistrationTests : TestBase
{
    private readonly InternalCommandMapper _mapper;

    public InternalCommandRegistrationTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _mapper = GetService<InternalCommandMapper>();
    }

    [Theory(DisplayName = nameof(Internal_commands_are_registered))]
    [MemberData(nameof(GetInternalCommands))]
    public void Internal_commands_are_registered(Type internalCommand)
    {
        Assert.True(IsRegistered(internalCommand));
    }

    private static IEnumerable<object[]> GetInternalCommands()
    {
        var allTypes = ApplicationAssemblies.Application.GetTypes().Concat(ApplicationAssemblies.Infrastructure.GetTypes());
        return allTypes
            .Where(x => x.BaseType == typeof(InternalCommand))
                .Select(x => new[] { x });
    }

    private bool IsRegistered(Type commandType)
    {
        if (commandType == null) throw new ArgumentNullException(nameof(commandType));
        try
        {
            _mapper.GetByType(commandType);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
