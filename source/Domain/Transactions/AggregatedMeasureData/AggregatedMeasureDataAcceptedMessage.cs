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

using System.Text.Json;
using Domain.Actors;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.AccountingPointCharacteristics.MarketEvaluationPointDetails;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;

namespace Domain.Transactions.AggregatedMeasureData;

public class AggregatedMeasureDataAcceptedMessage : OutgoingMessage
{
    private AggregatedMeasureDataAcceptedMessage(ActorNumber receiverId, TransactionId transactionId, string businessReason, Series series)
        : base(DocumentType.RequestAggregatedMeasureData, receiverId, transactionId, businessReason, MarketRole.MeteredDataResponsible, DataHubDetails.IdentificationNumber, MarketRole.MeteringPointAdministrator, JsonSerializer.Serialize(series))
    {
        Series = series;
    }

    public Series Series { get; }

    public static AggregatedMeasureDataAcceptedMessage Create(
        ProcessId processId,
        ActorProvidedId actorProvidedId,
        BusinessReason businessReason)
    {
        ArgumentNullException.ThrowIfNull(processId);
        ArgumentNullException.ThrowIfNull(businessReason);
        ArgumentNullException.ThrowIfNull(actorProvidedId);
        var series = new TimeSeries();

        return new AggregatedMeasureDataAcceptedMessage(ActorNumber.Create(actorProvidedId.Id), TransactionId.Create(processId.Id), businessReason.ToString(), series);
    }
}
