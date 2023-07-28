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

using Domain.Actors;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using NodaTime;

namespace IntegrationTests.Application.Transactions.AggregatedMeasureData;

public class AggregatedMeasureDataProcessBuilder
{
    private readonly string _balanceResponsiblePartyMarketParticipantId = "5799999933318";
    private readonly Instant _endDateAndOrTimeDateTime = SystemClock.Instance.GetCurrentInstant();
    private readonly string _energySupplierMarketParticipantId = "5790001330552";
    private readonly string _marketEvaluationSettlementMethod = "D01";
    private readonly string _meteringGridAreaDomainId = "244";
    private readonly string _meteringPointType = string.Empty;
    private readonly string _senderId = "1234567891234567";
    private readonly string _serieId = "123353185";
    private readonly string _settlementSeriesVersion = "2";
    private readonly Instant _startDateAndOrTimeDateTime = SystemClock.Instance.GetCurrentInstant();

    internal static AggregatedMeasureDataProcess Build(ProcessId processId)
    {
        return new AggregatedMeasureDataProcessBuilder().CreateProcess(processId);
    }

    private AggregatedMeasureDataProcess CreateProcess(ProcessId processId)
    {
        return new AggregatedMeasureDataProcess(
            processId,
            BusinessTransactionId.Create(_serieId),
            ActorNumber.Create(_senderId),
            _settlementSeriesVersion,
            _meteringPointType,
            _marketEvaluationSettlementMethod,
            _startDateAndOrTimeDateTime,
            _endDateAndOrTimeDateTime,
            _meteringGridAreaDomainId,
            _energySupplierMarketParticipantId,
            _balanceResponsiblePartyMarketParticipantId);
    }
}
