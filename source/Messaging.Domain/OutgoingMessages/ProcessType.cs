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

using Messaging.Domain.SeedWork;

namespace Messaging.Domain.OutgoingMessages;

public sealed class ProcessType : EnumerationType
{
    public static readonly ProcessType MoveIn = new(0, nameof(MoveIn), "E65");
    public static readonly ProcessType BalanceFixing = new(1, nameof(BalanceFixing), "D04");

    private ProcessType(int id, string name, string code)
     : base(id, name)
    {
        Code = code;
    }

    public string Code { get; }
}
