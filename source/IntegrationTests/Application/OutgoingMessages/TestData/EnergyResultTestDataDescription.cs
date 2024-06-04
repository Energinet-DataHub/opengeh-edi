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

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages.TestData;

/// <summary>
/// Contains information to prepare test input from a file, as well as
/// information to drive and verify the expected behaviour, for a certain scenario.
/// </summary>
public class EnergyResultTestDataDescription
{
    public EnergyResultTestDataDescription()
    {
        TestFilePath = Path.Combine("Application", "OutgoingMessages", "TestData", "balance_fixing_01-11-2022_01-12-2022_ga_543.csv");
    }

    /// <summary>
    /// Relative path to test file.
    /// </summary>
    public string TestFilePath { get; }

    /// <summary>
    /// Calculation id matching test file content.
    /// </summary>
    public Guid CalculationId => Guid.Parse("e7a26e65-be5e-4db0-ba0e-a6bb4ae2ef3d");

    /// <summary>
    /// Expected outgoing messages based on test file content.
    /// </summary>
    public int ExpectedOutgoingMessagesCount => 5;
}
