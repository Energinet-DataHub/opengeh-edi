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

public static class MasterDataTranslation
{
    static MasterDataTranslation()
    {
        Translations.Add(string.Empty, string.Empty);
        AddMeteringPointTypes();
        AddSettlementMethods();
        AddMeteringMethods();
        AddConnectionStates();
        AddReadingPeriodicities();
        AddNetSettlementGroups();
        AddConnectionTypes();
        AddDisconnectionTypes();
        AddAssetTypes();
        AddProductTypes();
        AddMeasurementUnitType();
        Translations.Add("s", "5790001330590");
        Translations.Add("Taridasdff", "5790001330590");
    }

    public static Dictionary<string, string> Translations { get; } = new();

    public static string TranslateToNextReadingDate(string scheduledMeterReadingDate)
    {
        return "--" + string.Concat(
                        scheduledMeterReadingDate
                            .AsSpan(0, 2).ToString())
                    + "-" +
                    string.Concat(
                        scheduledMeterReadingDate.AsSpan(2).ToString());
    }

    private static void AddMeasurementUnitType()
    {
        Translations.Add("KVArh", "K3");
        Translations.Add("KWh", "KWH");
        Translations.Add("KW", "KWT");
        Translations.Add("MW", "MAW");
        Translations.Add("MWh", "MWH");
        Translations.Add("Tonne", "TNE");
        Translations.Add("MVAr", "Z03");
        Translations.Add("Ampere", "AMP");
        Translations.Add("STK", "H87");
        Translations.Add("DanishTariffCode", "Z14");
    }

    private static void AddProductTypes()
    {
        Translations.Add("EnergyActive", "8716867000030");
        Translations.Add("EnergyReactive", "8716867000047");
        Translations.Add("PowerActive", "8716867000016");
        Translations.Add("PowerReactive", "8716867000023");
        Translations.Add("FuelQuantity", "5790001330606");
        Translations.Add("Tariff", "5790001330590");
    }

    private static void AddAssetTypes()
    {
        Translations.Add("SteamTurbineWithBackPressureMode", "D01");
        Translations.Add("GasTurbine", "D02");
        Translations.Add("CombinedCycle", "D03");
        Translations.Add("CombustionEngineGas", "D04");
        Translations.Add("SteamTurbineWithCondensation", "D05");
        Translations.Add("Boiler", "D06");
        Translations.Add("StirlingEngine", "D07");
        Translations.Add("FuelCells", "D10");
        Translations.Add("PhotovoltaicCells", "D11");
        Translations.Add("WindTurbines", "D12");
        Translations.Add("HydroelectricPower", "D13");
        Translations.Add("WavePower", "D14");
        Translations.Add("DispatchableWindTurbines", "D17");
        Translations.Add("DieselCombustionEngine", "D19");
        Translations.Add("BioCombustionEngine", "D20");
        Translations.Add("NoTechnology", "D98");
        Translations.Add("UnknownTechnology", "D99");
    }

    private static void AddConnectionTypes()
    {
        Translations.Add("Direct", "D01");
        Translations.Add("Installation", "D02");
    }

    private static void AddDisconnectionTypes()
    {
        Translations.Add("Remote", "D01");
        Translations.Add("Manual", "D02");
    }

    private static void AddNetSettlementGroups()
    {
        Translations.Add("Zero", "0");
        Translations.Add("One", "1");
        Translations.Add("Two", "2");
        Translations.Add("Three", "3");
        Translations.Add("Six", "6");
        Translations.Add("Ninetynine", "99");
    }

    private static void AddReadingPeriodicities()
    {
        Translations.Add("Yearly", "P1Y");
        Translations.Add("Monthly", "P1M");
        Translations.Add("Hourly", "PT1H");
        Translations.Add("Quarterly", "PT15M");
    }

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
