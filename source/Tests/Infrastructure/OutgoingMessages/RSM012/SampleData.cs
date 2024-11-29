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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM012;

internal static class SampleData
{
    #region header attributes

    public static string MessageId => "11111111111111111111111111111111";

    public static BusinessReason BusinessReason => BusinessReason.PeriodicMetering;

    public static string SenderActorNumber => "5790000000000";

    public static string SenderActorRole => ActorRole.MeteredDataAdministrator.Code;

    public static string ReceiverActorNumber => "5790000044444444";

    public static string ReceiverActorRole => ActorRole.MeteredDataResponsible.Code;

    public static Instant TimeStamp => InstantPattern.General.Parse("2022-02-12T23:00:00Z").Value;

    #endregion

    #region series attributes

    public static TransactionId TransactionId => TransactionId.From("11111111111111111111111111111111");

    public static string MeteringPointNumber => "579999993331812345";

    public static string MeteringPointType => "E17";

    public static string? OriginalTransactionIdReferenceId => "C1875000";

    public static string Product => "8716867000030";

    public static string QuantityMeasureUnit => MeasurementUnit.Kwh.Code;

    public static string RegistrationDateTime => "2022-12-17T07:30:00Z";

    public static string Resolution => "PT1H";

    public static string StartedDateTime => "2022-08-15T22:00Z";

    public static string EndedDateTime => "2022-08-15T04:00Z";

    public static IReadOnlyList<PointActivityRecord> Points => new List<PointActivityRecord>
    {
        new(1, "A03", 242),
        new(2, null, 242),
        new(3, null, 222),
        new(4, null, 202),
        new(5, null, 191),
        new(6, "A02", null),
    };

    #endregion
}
