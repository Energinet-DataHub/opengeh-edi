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
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.Tests.Factories;

internal static class SampleData
{
    public static string MessageId => "12345678";

    public static string SenderId => "1234567890123";

    public static ActorRole SenderRole => ActorRole.MeteredDataAdministrator;

    public static string ReceiverId => "1234567890987";

    public static ActorRole ReceiverRole => ActorRole.BalanceResponsibleParty;

    public static Instant CreationDate => InstantPattern.General.Parse("2022-02-12T23:00:00Z").Value;

    public static Guid TransactionId => Guid.Parse("4E85A732-85FD-4D92-8FF3-72C052802716");

    public static string ReasonCode => "A02";

    public static BusinessReason BusinessReason => BusinessReason.BalanceFixing;

    public static string SerieReasonCode => "E18";

    public static string SerieReasonMessage => "Det virker ikke!";

    public static string OriginalTransactionId => "4E85A73285FD4D928FF372C052802717";
}
