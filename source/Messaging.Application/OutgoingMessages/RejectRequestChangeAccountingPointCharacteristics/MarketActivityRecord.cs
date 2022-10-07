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

using System.Collections.Generic;
using Messaging.Application.OutgoingMessages.RejectRequestChangeOfSupplier;

namespace Messaging.Application.OutgoingMessages.RejectRequestChangeAccountingPointCharacteristics;

public class MarketActivityRecord
{
    public MarketActivityRecord(string id, string businessProcessReference, string originalTransactionId, string marketEvaluationPointId, IEnumerable<Reason> reasons)
    {
        Id = id;
        BusinessProcessReference = businessProcessReference;
        OriginalTransactionId = originalTransactionId;
        MarketEvaluationPointId = marketEvaluationPointId;
        Reasons = reasons;
    }

    public string Id { get; }

    public string BusinessProcessReference { get; }

    public string OriginalTransactionId { get; }

    public string MarketEvaluationPointId { get; }

    public IEnumerable<Reason> Reasons { get; }
}
