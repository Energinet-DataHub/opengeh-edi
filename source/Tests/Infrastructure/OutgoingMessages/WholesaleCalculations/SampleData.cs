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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using NodaTime;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.WholesaleCalculations;

internal static class SampleData
{
    #region header attributes

    public static Guid MessageId => Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static ActorNumber SenderId => ActorNumber.Create("5790000000000");

    public static ActorNumber ReceiverId => ActorNumber.Create("579000004444");

    public static string Timestamp => "2022-12-20T23:00:00Z";

    #endregion

    #region series attributes

    public static Guid TransactionId => Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static int Version => 1;

    public static string GridAreaCode => "123";

    public static ActorNumber EnergySupplier => ActorNumber.Create("5790000000000");

    public static ActorNumber ChargeOwner => ActorNumber.Create("5790000000111");

    public static MonthlyAmountPerChargeResultProducedV1.Types.ChargeType ChargeType => MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Tariff;

    public static MonthlyAmountPerChargeResultProducedV1.Types.CalculationType CalculationType => MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing;

    public static MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit QuantityUnit => MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit.Kwh;

    public static MonthlyAmountPerChargeResultProducedV1.Types.Currency Currency => MonthlyAmountPerChargeResultProducedV1.Types.Currency.Dkk;

    public static string ChargeCode => "123456";

    public static Instant PeriodStartUtc => Instant.FromUtc(2022, 1, 1, 0, 0);

    public static Instant PeriodEndUtc => Instant.FromUtc(2022, 2, 1, 0, 0);

    #endregion
}
