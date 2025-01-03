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

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;

public abstract class ValidationError
{
    protected ValidationError(string message, string code, string ebixMessage, string ebixCode, string? target = null)
    {
        Message = message;
        Code = code;
        EbixMessage = ebixMessage;
        EbixCode = ebixCode;
        Target = target;
    }

    public string Message { get; }

    public string Code { get; }

    public string EbixMessage { get; }

    public string EbixCode { get; }

    public string? Target { get; }

    public override string ToString()
    {
        return $"'Code: {Code}, Message: {Message}, Target: {Target}'";
    }
}
