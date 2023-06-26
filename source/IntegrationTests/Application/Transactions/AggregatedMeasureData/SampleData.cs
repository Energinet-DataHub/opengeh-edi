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

namespace IntegrationTests.Application.Transactions.AggregatedMeasureData;

public static class SampleData
{
    internal static string SerieId => "123353185";

    internal static string SettlementSeriesVersion => "D01";

    internal static string MarketEvaluationPointType => "E17";

    internal static string MarketEvaluationSettlementMethod => "D01";

    internal static string StartDateAndOrTimeDateTime => "2022-06-17T22:00:00Z";

    internal static string EndDateAndOrTimeDateTime => "2022-07-22T22:00:00Z";

    internal static string MeteringGridAreaDomainId => "244";

    internal static string BiddingZoneDomainId => "10YDK-1--------M";

    internal static string EnergySupplierMarketParticipantId => "5790001330552";

    internal static string BalanceResponsiblePartyMarketParticipantId => "5799999933318";

    internal static string RequestedByActorId => "123456987";
}
