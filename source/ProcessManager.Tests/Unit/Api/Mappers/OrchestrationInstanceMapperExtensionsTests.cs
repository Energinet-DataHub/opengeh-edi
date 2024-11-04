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

using System.Text.Json;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationDescription;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Api.Mappers;
using Energinet.DataHub.ProcessManager.Api.Model;
using FluentAssertions;
using NodaTime;

namespace Energinet.DataHub.ProcessManager.Tests.Unit.Api.Mappers;

public class OrchestrationInstanceMapperExtensionsTests
{
    /// <summary>
    /// Even the 'ParameterValue' is mapped in a way that allows us to serialize the object
    /// and deserialize it to a strongly typed orchestration instance with parmeters.
    /// </summary>
    [Fact]
    public void MapToDto_WhenOrchestrationInstance_CreateOrchestrationInstanceDtoThatCanBeFullySerializedToJson()
    {
        var orchestrationInstance = CreateOrchestrationInstance();

        // Act
        var actualDto = orchestrationInstance.MapToDto();
        var dtoAsJson = JsonSerializer.Serialize(actualDto);

        // Assert
        var typedDto = JsonSerializer.Deserialize<OrchestrationInstanceTypedDto<TestOrchestrationParameter>>(dtoAsJson);
        typedDto!.ParameterValue!.TestString.Should().NotBeNull();
        typedDto!.ParameterValue!.TestInt.Should().NotBeNull();
    }

    private static OrchestrationInstance CreateOrchestrationInstance()
    {
        var orchestrationDescriptionId = new OrchestrationDescriptionId(Guid.NewGuid());

        var existingOrchestrationInstance = new OrchestrationInstance(
            orchestrationDescriptionId,
            SystemClock.Instance);

        var step1 = new OrchestrationStep(
            existingOrchestrationInstance.Id,
            SystemClock.Instance,
            "Test step 1",
            0);

        var step2 = new OrchestrationStep(
            existingOrchestrationInstance.Id,
            SystemClock.Instance,
            "Test step 2",
            1,
            step1.Id);

        var step3 = new OrchestrationStep(
            existingOrchestrationInstance.Id,
            SystemClock.Instance,
            "Test step 3",
            2,
            step2.Id);

        existingOrchestrationInstance.Steps.Add(step1);
        existingOrchestrationInstance.Steps.Add(step2);
        existingOrchestrationInstance.Steps.Add(step3);

        existingOrchestrationInstance.ParameterValue.SetFromInstance(new TestOrchestrationParameter
        {
            TestString = "Test string",
            TestInt = 42,
        });

        return existingOrchestrationInstance;
    }

    private class TestOrchestrationParameter
    {
        public string? TestString { get; set; }

        public int? TestInt { get; set; }
    }
}
