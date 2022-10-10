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

namespace Messaging.Application.IncomingMessages.RequestChangeAccountPointCharacteristics;

public class MarketActivityRecord : IMarketActivityRecord
{
    public string Id { get; init; } = string.Empty;

    public string EffectiveDate { get; init; } = string.Empty;

    public MarketEvaluationPoint MarketEvaluationPoint { get; init; } = new MarketEvaluationPoint();
}

public class MarketEvaluationPoint
{
    public string Id { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string SettlementMethod { get; init; } = string.Empty;

    public string MeteringMethod { get; init; } = string.Empty;

    public string ConnectionState { get; init; } = string.Empty;

    public string ReadCycle { get; init; } = string.Empty;
}
