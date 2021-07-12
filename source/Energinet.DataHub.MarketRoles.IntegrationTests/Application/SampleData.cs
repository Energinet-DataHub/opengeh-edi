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
using NodaTime;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application
{
    public static class SampleData
    {
        public static string GsrnNumber => "571234567891234568";

        public static string ConsumerSSN => "2601211234";

        public static string ConsumerVAT => "10000000";

        public static string GlnNumber => "5790000555550";

        public static string Transaction => Guid.NewGuid().ToString();

        public static string ConsumerName => "Test Testesen";

        internal static string MoveInDate => Instant.FromDateTimeUtc(DateTime.UtcNow.AddHours(1)).ToString();
    }
}
