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

namespace Messaging.Application.Transactions;

public class BusinessRequestResult
{
    private BusinessRequestResult(string processId)
    {
        ProcessId = processId;
    }

    private BusinessRequestResult(IReadOnlyCollection<string> validationErrors)
    {
        ValidationErrors = validationErrors;
    }

    public bool Success => ValidationErrors.Count == 0;

    public string? ProcessId { get; }

    public IReadOnlyCollection<string> ValidationErrors { get; } = new List<string>();

    public static BusinessRequestResult Failure(params string[] validationErrors)
    {
        return new BusinessRequestResult(validationErrors);
    }

    public static BusinessRequestResult Succeeded(string processId)
    {
        if (string.IsNullOrEmpty(processId)) throw new ArgumentNullException(nameof(processId));
        return new BusinessRequestResult(processId);
    }
}
