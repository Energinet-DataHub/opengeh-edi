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

namespace Messaging.Tests.Domain.Transactions.MoveIn;

internal class SampleData
{
    internal static Guid TransactionId => Guid.Parse("17DE02FC-6A83-436F-BC89-779ABBD6AB35");

    internal static string ProcessId => "1F4D6E69-5C0D-461C-AB52-E34EF77E24D8";

    internal static string MarketEvaluationPointId => "BFBB73FD-6CC1-4EC1-BA97-568589BF85AD";

    internal static Instant EffectiveDate => SystemClock.Instance.GetCurrentInstant();

    internal static string CurrentEnergySupplierId => "EDE9D5B6-2634-438D-A79F-277E556AF7F6";

    internal static string StartedByMessageId => "9FFE7929-4416-466E-9220-8B500E62423E";

    internal static string NewEnergySupplierId => "3604FB93-698D-4C40-A97B-A616D9778D6A";

    internal static string ConsumerId => "12345678";

    internal static string ConsumerName => "John Doe";

    internal static string ConsumerIdType => "ARR";

    internal static string RequestedByActorNumber => "1234567890123";

    internal static string SenderId => "1234567890126";

    internal static string GridOperatorNumber => "1234567890122";
}
