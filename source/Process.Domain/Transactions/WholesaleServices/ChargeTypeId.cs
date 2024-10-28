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
using System.Text.Json.Serialization;

namespace Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;

public record ChargeTypeId
{
    [JsonConstructor]
    private ChargeTypeId(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; }

    public static ChargeTypeId New()
    {
        return new ChargeTypeId(Guid.NewGuid());
    }

    public static ChargeTypeId Create(Guid id)
    {
        return new ChargeTypeId(id);
    }
}
