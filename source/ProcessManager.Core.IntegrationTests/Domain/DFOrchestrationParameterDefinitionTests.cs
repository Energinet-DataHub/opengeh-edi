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

using Energinet.DataHub.ProcessManagement.Core.Domain;
using FluentAssertions;

namespace Energinet.DataHub.ProcessManager.Core.IntegrationTests.Domain;

public class DFOrchestrationParameterDefinitionTests
{
    [Fact]
    public void GivenRecordType_WhenSetParameterFromType_CanValidateParameter()
    {
        // TODO: Look at generating Json Schema => https://www.newtonsoft.com/jsonschema/help/html/GeneratingSchemas.htm
        // WARNING: Might require a license, so we might have to look for another solution, like: https://docs.json-everything.net/schema/basics/

        // Arrange
        var sut = new DFOrchestrationParameterDefinition();

        // Act
        sut.SetFromType<OrchestrationParameterExample>();

        // Assert
        var parameter = new OrchestrationParameterExample(DateTimeOffset.Now, true);
        var isValid = sut.IsValidParameterValue(parameter);

        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Example orchestration parameter for testing purposes.
    /// DOES NOT work if the parameter use the 'NodaTime.Instant' type.
    /// </summary>
    public sealed record OrchestrationParameterExample(
        DateTimeOffset ScheduledAt,
        bool IsInternal);
}
