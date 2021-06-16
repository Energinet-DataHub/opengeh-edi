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

namespace Energinet.DataHub.MarketRoles.Domain.Consumers.Rules
{
    internal class CvrNumberFormatRule : IBusinessRule
    {
        private const int RequiredIdLength = 8;
        private const int LowerBound = 10000000;
        private const int UpperBound = 99999999;
        private readonly string? _cvrValue;

        public CvrNumberFormatRule(string? cvrValue)
        {
            _cvrValue = cvrValue;
        }

        public bool IsBroken => !IsValidCvrNumber();

        public ValidationError Error => new CvrNumberFormatRuleError();

        private static bool LengthIsValid(string cvrValue)
        {
            return cvrValue.Length == RequiredIdLength;
        }

        private static bool RangeIsValid(long cvrNumber)
        {
            return cvrNumber >= LowerBound && cvrNumber <= UpperBound;
        }

        private bool IsValidCvrNumber()
        {
            return !string.IsNullOrEmpty(_cvrValue)
                && NumberHelper.ParseNumber(_cvrValue, out var cvrNumber)
                && LengthIsValid(_cvrValue)
                && RangeIsValid(cvrNumber);
        }
    }
}
