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
using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace Energinet.DataHub.MarketRoles.Domain.MeteringPoints
{
    public class BusinessProcessId : ValueObject
    {
        public BusinessProcessId(Guid value)
        {
            Value = value;
        }

        public Guid Value { get; }

        public static BusinessProcessId New()
        {
            return new BusinessProcessId(Guid.NewGuid());
        }

        public static BusinessProcessId Create(string value)
        {
            return new BusinessProcessId(Guid.Parse(value));
        }

        public static BusinessProcessId Create(Guid value)
        {
            return new BusinessProcessId(value);
        }
    }
}
