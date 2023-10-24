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
using Energinet.DataHub.EDI.Domain.Actors;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.AggregationWholesaleResult;

internal static class SampleData
{
    public static string EnergySupplierNumber => "5790001330552";

    public static string BalanceResponsibleNumber => "6790001330551";

    public static string MessageId => "12345678";

    public static string SenderId => "1234567890123";

    public static MarketRole SenderRole => MarketRole.MeteringDataAdministrator;

    public static string ReceiverId => "1234567890987";

    public static MarketRole ReceiverRole => MarketRole.BalanceResponsibleParty;

    public static string Timestamp => "2022-12-20T23:00:00Z";

    public static string GridAreaCode => "234";

    public static Guid TransactionId => Guid.Parse("4E85A732-85FD-4D92-8FF3-72C052802716");

    public static Instant StartOfPeriod => InstantPattern.General.Parse("2022-02-12T23:00:00Z").Value;

    public static Instant EndOfPeriod => InstantPattern.General.Parse("2022-02-13T23:00:00Z").Value;

    public static string OriginalTransactionIdReference => "23252946094_24731676818";
}
