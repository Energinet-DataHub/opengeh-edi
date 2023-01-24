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
using Messaging.IntegrationTests.Factories;
using NodaTime;

namespace Messaging.IntegrationTests.Application.Transactions.UpdateCustomer;

internal static class SampleData
{
    internal static Guid TransactionId => Guid.Parse("a761ca16-7fa6-4a04-8420-9f7bda780362");

    internal static string MarketEvaluationPointNumber => "571234567891234568";

    internal static Instant EffectiveDate => EffectiveDateFactory.InstantAsOfToday();
}
