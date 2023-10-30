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

using Energinet.DataHub.EDI.Domain.Common;

namespace Energinet.DataHub.EDI.Domain.Transactions.Aggregations;

public class ReasonCode : EnumerationCodeType
{
    public static readonly ReasonCode FullyAccepted = new(0, nameof(FullyAccepted), "A01");
    public static readonly ReasonCode FullyRejected = new(1, nameof(FullyRejected), "A02");

    public ReasonCode(int id, string name, string code)
        : base(id, name, code)
    {
    }
}
