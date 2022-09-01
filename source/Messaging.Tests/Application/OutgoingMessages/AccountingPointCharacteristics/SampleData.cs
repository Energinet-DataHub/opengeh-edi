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
using Messaging.Application.OutgoingMessages.AccountingPointCharacteristics;
using Messaging.Application.OutgoingMessages.AccountingPointCharacteristics.MarketEvaluationPointDetails;
using Messaging.Domain.MasterData;
using Messaging.Infrastructure.Configuration;

namespace Messaging.Tests.Application.OutgoingMessages.AccountingPointCharacteristics;

public class SampleData
{
    private readonly SystemDateTimeProvider _systemDateTimeProvider;

    public SampleData()
    {
        _systemDateTimeProvider = new SystemDateTimeProvider();
    }

    public MarketActivityRecord CreateMarketActivityRecord()
    {
        return new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            _systemDateTimeProvider.Now(),
            CreateMarketEvaluationPoint());
    }

    private static Address CreateAddress()
    {
        return new Address(
            new StreetDetail("0304", "Streetname", "1", "2", "3"),
            new TownDetail("0526", "TownName", "Section", "DK"),
            "7000");
    }

    private MarketEvaluationPoint CreateMarketEvaluationPoint()
    {
        return new MarketEvaluationPoint(
            new Mrid("FakeId", "A10"),
            new Mrid("FakeId", "A10"),
            "E17",
            "E02",
            "D01",
            "D01",
            "PT1H",
            "6",
            ReadingDate.Create("1217"),
            new Mrid("031", "NDK"),
            new Mrid("151", "NDK"),
            new Mrid("031", "NDK"),
            new Mrid("FakeId", "A10"),
            new UnitValue("1300", "KWT"),
            "D01",
            "D01",
            "D07",
            "false",
            new UnitValue("220", "KWT"),
            new UnitValue("32", "AMP"),
            Guid.NewGuid().ToString(),
            new Series("8716867000115", "KWH"),
            new Mrid("FakeId", "A10"),
            _systemDateTimeProvider.Now(),
            "Description",
            Guid.NewGuid().ToString(),
            CreateAddress(),
            "true",
            new RelatedMarketEvaluationPoint(new Mrid("FakeId", "A10"), "E17"),
            new RelatedMarketEvaluationPoint(new Mrid("FakeId", "A10"), "D06"));
    }
}
