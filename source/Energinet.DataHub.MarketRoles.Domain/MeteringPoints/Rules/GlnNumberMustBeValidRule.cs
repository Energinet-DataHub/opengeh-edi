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
using System.Linq;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules
{
    internal class GlnNumberMustBeValidRule : IBusinessRule
    {
        private const int RequiredIdLength = 13;
        private readonly string? _gsrnValue;

        public GlnNumberMustBeValidRule(string gsrnValue)
        {
            _gsrnValue = gsrnValue;
        }

        public bool IsBroken => !IsValidGsrnNumber();

        public ValidationError ValidationError => new GsrnNumberMustBeValidRuleError();

        private static bool IsEvenNumber(int index)
        {
            return index % 2 == 0;
        }

        private static int Parse(string input)
        {
            return int.Parse(input, System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        private bool IsValidGsrnNumber()
        {
            return LengthIsValid() && AllCharsAreDigits() && CheckSumIsValid();
        }

        private bool LengthIsValid()
        {
            return _gsrnValue?.Length == RequiredIdLength;
        }

        private bool AllCharsAreDigits()
        {
            return _gsrnValue!.All(char.IsDigit);
        }

        private bool CheckSumIsValid()
        {
            var definedChecksumDigit = Parse(_gsrnValue!.Substring(_gsrnValue.Length - 1));
            var calculatedChecksum = CalculateChecksum();
            return calculatedChecksum == definedChecksumDigit;
        }

        private int CalculateChecksum()
        {
            var sumOfOddNumbers = 0;
            var sumOfEvenNumbers = 0;

            for (var i = 1; i < _gsrnValue!.Length; i++)
            {
                var currentNumber = Parse(_gsrnValue.Substring(i - 1, 1));

                if (IsEvenNumber(i))
                {
                    sumOfEvenNumbers += currentNumber;
                }
                else
                {
                    sumOfOddNumbers += currentNumber;
                }
            }

            var sum = (sumOfEvenNumbers * 3) + sumOfOddNumbers;

            var equalOrHigherMultipleOf = (int)(Math.Ceiling(sum / 10.0) * 10);

            return equalOrHigherMultipleOf - sum;
        }
    }
}
