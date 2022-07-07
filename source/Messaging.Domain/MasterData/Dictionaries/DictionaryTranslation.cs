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

namespace Messaging.Domain.MasterData.Dictionaries;

public static class DictionaryTranslation
{
    static DictionaryTranslation()
    {
        Translations.Add(string.Empty, string.Empty);
        AddMeteringPointTypes();
        AddSettlementMethods();
        AddMeteringMethods();
        AddConnectionStates();
        Translations.Add("Yearly", "P1Y");
        Translations.Add("Monthly", "P1M");
        Translations.Add("Hourly", "PT1H");
        Translations.Add("Quarterly", "PT15M");
    }

    public static Dictionary<string, string> Translations { get; } = new();

    private static void AddConnectionStates()
    {
        Translations.Add("ClosedDown", "D02");
        Translations.Add("New", "D03");
        Translations.Add("Connected", "E22");
        Translations.Add("Disconnected", "E23");
    }

    private static void AddMeteringMethods()
    {
        Translations.Add("Physical", "D01");
        Translations.Add("Virtual", "D02");
        Translations.Add("Calculated", "D03");
    }

    private static void AddSettlementMethods()
    {
        Translations.Add("Flex", "D01");
        Translations.Add("NonProfiled", "E02");
        Translations.Add("Profiled", "E01");
    }

    private static void AddMeteringPointTypes()
    {
        Translations.Add("Consumption", "E17");
        Translations.Add("Production", "E18");
    }
}
