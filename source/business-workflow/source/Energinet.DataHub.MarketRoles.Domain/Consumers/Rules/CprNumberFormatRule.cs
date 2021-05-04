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

using Energinet.DataHub.MarketRoles.Domain.Helpers;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using NodaTime.Text;

namespace Energinet.DataHub.MarketRoles.Domain.Consumers.Rules
{
    internal class CprNumberFormatRule : IBusinessRule
    {
        private const int RequiredIdLength = 10;
        private readonly string? _cprValue;
        private readonly LocalDatePattern _datePattern = LocalDatePattern.CreateWithInvariantCulture("ddMMyy");

        public CprNumberFormatRule(string? cprValue)
        {
            _cprValue = cprValue;
        }

        public bool IsBroken => !IsValidCprNumber();

        public string Message => $"Invalid CPR number.";

        private static bool LengthIsValid(string cprValue)
        {
            return cprValue.Length == RequiredIdLength;
        }

        private bool IsValidCprNumber()
        {
            return !string.IsNullOrEmpty(_cprValue)
                   && NumberHelper.ParseNumber(_cprValue, out _)
                   && LengthIsValid(_cprValue)
                   && DateIsValid(_cprValue);
        }

        private bool DateIsValid(string cprValue)
        {
            return _datePattern.Parse(cprValue.Substring(0, 6)).Success;
        }
    }
}
