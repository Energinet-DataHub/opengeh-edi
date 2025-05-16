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

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM018;

public interface IAssertMissingMeasurementDocument
{
    #region Header assertions
    Task<IAssertMissingMeasurementDocument> DocumentIsValidAsync();

    Task<IAssertMissingMeasurementDocument> HasBusinessReason(BusinessReason businessReason);

    Task<IAssertMissingMeasurementDocument> HasSenderId(ActorNumber actorNumber);

    Task<IAssertMissingMeasurementDocument> HasSenderRole(ActorRole actorRole);

    Task<IAssertMissingMeasurementDocument> HasReceiverId(ActorNumber actorNumber);

    Task<IAssertMissingMeasurementDocument> HasReceiverRole(ActorRole actorRole);

    Task<IAssertMissingMeasurementDocument> HasTimestamp(Instant timestamp);
    #endregion

    #region Series assertions
    Task<IAssertMissingMeasurementDocument> HasTransactionId(
        int seriesIndex,
        TransactionId expectedTransactionId);

    /// <summary>
    /// Even tho the CIM schemas allows a list of metering points, the Ebix schema does not.
    /// Hence, we will never expect more than one metering point in a series.
    /// </summary>
    /// <param name="seriesIndex"></param>
    /// <param name="meteringPointNumber"></param>
    Task<IAssertMissingMeasurementDocument> HasMeteringPointNumber(
        int seriesIndex,
        string meteringPointNumber);

    Task<IAssertMissingMeasurementDocument> HasMissingDate(
        int seriesIndex,
        Instant missingDate);
    #endregion

}
