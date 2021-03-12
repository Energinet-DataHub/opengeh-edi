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

namespace Energinet.DataHub.MarketData.Domain.Helpers
{
    internal class NumberHelper
    {
        /// <summary>
        /// Checks if the value is an int and also outputs the parsed number.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="parsedNumber">The parsed integer</param>
        /// <returns>Boolean to indicate if the value is an int or not.</returns>
        public static bool ParseNumber(string value, out long parsedNumber)
        {
            return long.TryParse(value, out parsedNumber);
        }
    }
}
