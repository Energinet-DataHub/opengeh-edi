﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Collections.Generic;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Rules;
using Energinet.DataHub.MarketData.Domain.SeedWork;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints
{
    public class GsrnNumber : ValueObject
    {
        private GsrnNumber(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static GsrnNumber Create(string gsrn)
        {
            var formattedValue = gsrn?.Trim();

            if (string.IsNullOrWhiteSpace(formattedValue))
            {
                throw new ArgumentException($"'{nameof(gsrn)}' cannot be null or whitespace", nameof(gsrn));
            }

            ThrowIfInvalid(formattedValue);
            return new GsrnNumber(formattedValue);
        }

        public static BusinessRulesValidationResult CheckRules(string gsrnValue)
        {
            return new BusinessRulesValidationResult(new List<IBusinessRule>()
            {
                new GsrnNumberMustBeValidRule(gsrnValue),
            });
        }

        public override string ToString()
        {
            return Value;
        }

        private static void ThrowIfInvalid(string gsrnValue)
        {
            var result = CheckRules(gsrnValue);
            if (result.AreAnyBroken)
            {
                throw new InvalidMeteringPointIdRuleException("Invalid metering point id.");
            }
        }
    }
}
