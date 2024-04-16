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
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications.Handlers;
using Energinet.DataHub.EDI.Process.Domain.Commands;
using Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.InternalCommands;

public class InternalCommandRegistrationTests : TestBase
{
    private readonly InternalCommandMapper _mapper;

    public InternalCommandRegistrationTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _mapper = GetService<InternalCommandMapper>();
    }

    public static IEnumerable<object[]> GetInternalCommands()
    {
        var allTypes = typeof(EnqueueAcceptedEnergyResultMessageHandler).Assembly.GetTypes();

        return allTypes
            .Where(x => x.BaseType == typeof(InternalCommand))
            .Select(x => new[] { x });
    }

    [Theory(DisplayName = nameof(Internal_commands_are_registered))]
    [MemberData(nameof(GetInternalCommands))]
    public void Internal_commands_are_registered(Type internalCommand)
    {
        IsRegistered(internalCommand)
            .Should()
            .BeTrue($"internal command {internalCommand.Name} should have been registered");
    }

    private bool IsRegistered(Type commandType)
    {
        ArgumentNullException.ThrowIfNull(commandType);
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
