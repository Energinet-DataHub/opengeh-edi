﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.TestData;

/// <summary>
/// Contains information to prepare test input from a file, as well as
/// information to drive and verify the expected behaviour, for a certain scenario.
/// </summary>
public abstract class TestDataDescription
{
    protected TestDataDescription(string testFilename, string testFilenameWithAInvalidRow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testFilename);

        TestFilePath = Path.Combine("Behaviours", "TestData", testFilename);
        TestFilePathWithAInvalidRow = Path.Combine("Behaviours", "TestData", testFilenameWithAInvalidRow);
    }

    /// <summary>
    /// Relative path to test file.
    /// </summary>
    public string TestFilePath { get; }

    /// <summary>
    /// Relative path to test file with an invalid row.
    /// </summary>
    public string TestFilePathWithAInvalidRow { get; }

    /// <summary>
    /// Calculation id matching test file content.
    /// </summary>
    public abstract Guid CalculationId { get; }

    /// <summary>
    /// Grid area code matching test file content.
    /// </summary>
    public abstract IReadOnlyCollection<string> GridAreaCodes { get; }

    /// <summary>
    /// Expected calculation results based on test file content.
    /// </summary>
    public abstract int ExpectedCalculationResultsCount { get; }

    /// <summary>
    /// Period start/end matching the file content.
    /// </summary>
    public abstract Period Period { get; }
}
