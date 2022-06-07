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
using System.Linq;

namespace Messaging.Application.OutgoingMessages;

public sealed class ProcessType : EnumerationType
{
    public static readonly ProcessType MoveIn = new(0, nameof(MoveIn), "E03", "A01", "A02", new ProcessDetails("A01", "ConfirmRequestChangeOfSupplier"), new ProcessDetails("A02", "RejectRequestChangeOfSupplier"));
    public static readonly ProcessType Unknown = new(999, nameof(MoveIn), "Unknown", "Unknown", "UnknownA02", new ProcessDetails("Unknown", "Unknown"), new ProcessDetails("Unknown", "Unknown"));

    private ProcessType(int id, string name, string code, string reasonCodeForConfirm, string reasonCodeForReject, ProcessDetails confirm, ProcessDetails reject)
     : base(id, name)
    {
        ReasonCodeForConfirm = reasonCodeForConfirm;
        ReasonCodeForReject = reasonCodeForReject;
        Confirm = confirm;
        Reject = reject;
        Code = code;
    }

    public string Code { get; }

    public string ReasonCodeForConfirm { get; }

    public string ReasonCodeForReject { get; }

    public ProcessDetails Reject { get; }

    public ProcessDetails Confirm { get; }

    public static ProcessType FromCode(string code)
    {
        var processType = GetAll<ProcessType>().FirstOrDefault(p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        return processType ?? Unknown;
    }
}

public record ProcessDetails(string BusinessReasonCode, string DocumentType);
