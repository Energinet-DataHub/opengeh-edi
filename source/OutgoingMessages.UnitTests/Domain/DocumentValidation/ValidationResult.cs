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

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.DocumentValidation;

public class ValidationResult
{
    private ValidationResult(IReadOnlyCollection<string> validationErrors)
    {
        ValidationErrors = validationErrors;
    }

    private ValidationResult()
    {
    }

    public bool IsValid => ValidationErrors.Count == 0;

    public IReadOnlyCollection<string> ValidationErrors { get; } = new List<string>();

    public static ValidationResult Valid()
    {
        return new ValidationResult();
    }

    public static ValidationResult Invalid(IReadOnlyCollection<string> validationErrors)
    {
        return new ValidationResult(validationErrors);
    }
}
