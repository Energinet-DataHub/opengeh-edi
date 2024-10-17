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

using FluentAssertions;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests;

public sealed class SandboxTests
{
    [Fact]
    public void Given_SomeTestSetup_When_TestExecuted_Then_ItPasses()
    {
        // Arrange
        var firstNumber = 1d;
        var secondNumber = 2d;

        // Act
        var result = firstNumber + secondNumber;

        // Assert
        result.Should().Be(3d);
    }
}
