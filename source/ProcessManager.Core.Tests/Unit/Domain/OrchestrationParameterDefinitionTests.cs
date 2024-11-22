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

using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationDescription;
using FluentAssertions;

namespace Energinet.DataHub.ProcessManager.Core.Tests.Unit.Domain;

public class OrchestrationParameterDefinitionTests
{
    [Fact]
    public async Task GivenSetFromType_WhenValidatingInstanceOfSameType_ThenIsValid()
    {
        // Arrange
        var sut = new ParameterDefinition();
        sut.SetFromType<OrchestrationParameterExample01>();

        var instanceOfSameType = new OrchestrationParameterExample01(DateTimeOffset.Now, true);

        // Act
        var isValid = await sut.IsValidParameterValueAsync(instanceOfSameType);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task GivenSetFromType_WhenValidatingInstanceOfMatchingType_ThenIsValid()
    {
        // Arrange
        var sut = new ParameterDefinition();
        sut.SetFromType<OrchestrationParameterExample01>();

        var instanceOfMatchingType = new OrchestrationParameterExample02(DateTimeOffset.Now, true);

        // Act
        var isValid = await sut.IsValidParameterValueAsync(instanceOfMatchingType);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task GivenSetFromType_WhenValidatingInstanceOfAnotherType_ThenIsNotValid()
    {
        // Arrange
        var sut = new ParameterDefinition();
        sut.SetFromType<OrchestrationParameterExample01>();

        var instanceOfAnotherType = new OrchestrationParameterExample03(10, true);

        // Act
        var isValid = await sut.IsValidParameterValueAsync(instanceOfAnotherType);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Example orchestration parameter for testing purposes.
    /// DOES NOT work if the parameter use the 'NodaTime.Instant' type.
    /// </summary>
    public record OrchestrationParameterExample01(
        DateTimeOffset RunAt,
        bool IsInternal);

    /// <summary>
    /// Example orchestration parameter for testing purposes.
    /// </summary>
    public record OrchestrationParameterExample02(
        DateTimeOffset RunAt,
        bool IsInternal);

    /// <summary>
    /// Example orchestration parameter for testing purposes.
    /// </summary>
    public record OrchestrationParameterExample03(
        int Version,
        bool IsInternal);
}
