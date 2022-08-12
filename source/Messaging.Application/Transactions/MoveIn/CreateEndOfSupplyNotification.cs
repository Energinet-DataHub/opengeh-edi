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

using System.Text.Json.Serialization;
using Messaging.Application.Common.Commands;
using NodaTime;

namespace Messaging.Application.Transactions.MoveIn;

public class CreateEndOfSupplyNotification : InternalCommand
{
    [JsonConstructor]
    public CreateEndOfSupplyNotification(string transactionId, Instant effectiveDate, string marketEvaluationPointId, string energySupplierId)
    {
        TransactionId = transactionId;
        EffectiveDate = effectiveDate;
        MarketEvaluationPointId = marketEvaluationPointId;
        EnergySupplierId = energySupplierId;
    }

    public string TransactionId { get; }

    public Instant EffectiveDate { get; }

    public string MarketEvaluationPointId { get; }

    public string EnergySupplierId { get; }
}
