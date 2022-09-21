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
using Energinet.DataHub.EnergySupplying.IntegrationEvents;
using Google.Protobuf;

namespace IntegrationEvents.Tests;

public class ContractTests
{
    [Theory(DisplayName = nameof(Must_define_an_identifier))]
    [MemberData(nameof(GetAllIntegrationEvents))]
    public void Must_define_an_identifier(Type integrationEvent)
    {
        var hasIdField = integrationEvent?.GetField("IdFieldNumber") is not null;
        Assert.True(hasIdField);
    }

    private static IEnumerable<object[]> GetAllIntegrationEvents()
    {
        return typeof(ConsumerMovedIn)
            .Assembly
            .GetTypes()
            .Where(type => type.GetInterfaces().Contains(typeof(IMessage)))
            .Select(integrationEvent => new[] { integrationEvent });
    }
}
