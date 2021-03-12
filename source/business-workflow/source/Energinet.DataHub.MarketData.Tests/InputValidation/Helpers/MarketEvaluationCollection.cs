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
using System.Collections;
using System.Collections.Generic;
using GreenEnergyHub.Messaging.MessageTypes.Common;

namespace Energinet.DataHub.MarketData.Tests.InputValidation.Helpers
{
    public class MarketEvaluationCollection : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new MarketParticipant
                {
                    MRID = "TEST",
                },
            };

            yield return new object[]
            {
                new MarketParticipant
                {
                    MRID = "TEST",
                    Qualifier = "InvalidQualifier",
                },
            };

            yield return new object[]
            {
                new MarketParticipant
                {
                    MRID = "50000000", // CVR NUMBER
                    Qualifier = "ARR", // ARR = CPR NUMBER
                },
            };

            yield return new object[]
            {
                new MarketParticipant
                {
                    MRID = "2601211234",    // CPR NUMBER
                    Qualifier = "VA",       // VA = CVR NUMBER
                },
            };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
