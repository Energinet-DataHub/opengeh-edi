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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;

internal static class SampleData
{
    #region header attributes

    public static string MessageId => "11111111111111111111111111111111";

    public static ActorNumber SenderId => ActorNumber.Create("5790000000000");

    public static ActorNumber ReceiverId => ActorNumber.Create("5790000044444444");

    public static string Timestamp => "2022-12-20T23:00:00Z";

    #endregion

    #region series attributes

    public static TransactionId TransactionId => TransactionId.From("11111111111111111111111111111111");

    public static int Version => 1;

    public static string GridAreaCode => "123";

    public static ActorNumber EnergySupplier => ActorNumber.Create("5790000000000");

    public static ActorNumber ChargeOwner => ActorNumber.Create("5790000000111999");

    public static ChargeType ChargeType => ChargeType.Tariff;

    public static BusinessReason BusinessReason => BusinessReason.WholesaleFixing;

    public static MeasurementUnit MeasurementUnit => MeasurementUnit.KilowattHour;

    public static MeasurementUnit PriceMeasureUnit => MeasurementUnit.KilowattHour;

    public static Currency Currency => Currency.DanishCrowns;

    public static string ChargeCode => "123456";

    public static Resolution Resolution => Resolution.Monthly;

    public static int Quantity => 100;

    public static Instant PeriodStartUtc => Instant.FromUtc(2022, 1, 1, 0, 0);

    public static Instant PeriodEndUtc => Instant.FromUtc(2022, 2, 1, 0, 0);

    #endregion
}
