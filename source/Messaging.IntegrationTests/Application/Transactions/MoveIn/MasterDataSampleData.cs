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
using System.Globalization;
using NodaTime;
using Processing.Domain.MeteringPoints;

namespace Processing.IntegrationTests.Application
{
    internal static class MasterDataSampleData
    {
        public static string Administrator => "38406E2F-4045-45A3-A63C-3E8CCE07FEB9";

        public static string GsrnNumber => "571234567891234568";

        public static string GridAreaLinkId => "10A9E0E7-3906-4DC0-8CBD-A5C042A5C484";

        public static string StreetName => "Test Road 1";

        public static string PostCode => "8000";

        public static string CityName => "Aarhus";

        public static string CountryCode => "DK";

        public static bool IsActualAddress => true;

        public static string PhysicalStateName => PhysicalState.Connected.Name;

        public static string SubTypeName => "Physical";

        public static string TypeName => MeteringPointType.Consumption.Name;

        public static string PowerPlant => "571234567891234568";

        public static string LocationDescription => string.Empty;

        public static string ProductType => string.Empty;

        public static string? ParentRelatedMeteringPoint => null;

        public static string UnitType => string.Empty;

        public static string MeterNumber => "123456";

        public static string MeterReadingOccurence => string.Empty;

        public static int MaximumCurrent => 0;

        public static int MaximumPower => 230;

        public static string EffectiveDate => EffectiveDateNow();

        public static string SettlementMethod => "Flex";

        public static string NetSettlementGroup => "Six";

        public static string DisconnectionType => string.Empty;

        public static string ConnectionType => string.Empty;

        public static string AssetType => "WindTurbines";

        public static string Floor => string.Empty;

        public static string StreetCode => "0650";

        public static string BuildingNumber => "20";

        public static string Room => string.Empty;

        public static string CitySubdivision => string.Empty;

        public static int MunicipalityCode => default;

        public static string ScheduledMeterReadingDate => "0101";

        public static string GeoInfoReference => "{EB0ECFD2-97AD-48E3-8502-04C36AA7ACF8}";

        public static string Capacity => "1.2";

        public static string GlnNumber => "3963865549824";

        private static string EffectiveDateNow()
        {
            var currentDate = SystemClock.Instance.GetCurrentInstant().InUtc();
            var effectiveDate = Instant.FromUtc(
                currentDate.Year,
                currentDate.Month,
                currentDate.Day,
                23,
                0,
                0);

            return DaylightSavingsString(effectiveDate.ToDateTimeUtc());
        }

        private static string DaylightSavingsString(DateTime date)
        {
            var dateForString = new DateTime(
                date.Year,
                date.Month,
                date.Day,
                20,
                date.Minute,
                date.Second,
                date.Millisecond);

            var info = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var isDaylightSavingTime = info.IsDaylightSavingTime(dateForString);

            var retVal = dateForString.ToString(
                isDaylightSavingTime
                    ? $"yyyy'-'MM'-'dd'T'22':'mm':'ss'Z'"
                    : "yyyy'-'MM'-'dd'T'23':'mm':'ss'Z'",
                CultureInfo.InvariantCulture);

            return retVal;
        }
    }
}
