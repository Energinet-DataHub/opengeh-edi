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

namespace Energinet.DataHub.MarketRoles.Tests
{
    internal static class SampleData
    {
        internal static string GsrnNumber => "571234567891234568";

        internal static string ConsumerId => "2601211234";

        internal static string GlnNumber => "5790000555550";

        internal static string TranactionId => Guid.NewGuid().ToString();

        internal static string ConsumerName => "John Doe";

        internal static string ConsumerSocialSecurityNumber => "2601211234";

        internal static string ConsumerVATNumber => "10000000";

        internal static string Transaction => Guid.NewGuid().ToString();

        internal static string StartDate => SystemClock.Instance.GetCurrentInstant().ToString();
    }
}
