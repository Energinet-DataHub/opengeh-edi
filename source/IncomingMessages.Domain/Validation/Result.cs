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

using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;

public class Result
{
    private Result(string? messageId)
    {
        MessageId = messageId;
    }

    private Result(IReadOnlyCollection<ValidationError> errors)
    {
        Errors = errors;
    }

    public string? MessageId { get; }

    public bool Success => Errors.Count == 0;

    public IReadOnlyCollection<ValidationError> Errors { get; } = new List<ValidationError>();

    public static Result Failure(params ValidationError[] errors)
    {
        return new Result(errors);
    }

    public static Result Succeeded(string? messageId = null)
    {
        return new Result(messageId);
    }
}
