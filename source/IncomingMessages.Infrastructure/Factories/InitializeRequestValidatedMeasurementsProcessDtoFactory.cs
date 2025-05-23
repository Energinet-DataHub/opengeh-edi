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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Factories;

public static class InitializeRequestValidatedMeasurementsProcessDtoFactory
{
    public static InitializeRequestValidatedMeasurementsProcessDto Create(RequestValidatedMeasurementsMessageBase requestValidatedMeasurementsMessageBase)
    {
        ArgumentNullException.ThrowIfNull(requestValidatedMeasurementsMessageBase);

        var senderActorNumber = ActorNumber.Create(requestValidatedMeasurementsMessageBase.SenderNumber);
        var senderActorRole = ActorRole.FromCode(requestValidatedMeasurementsMessageBase.SenderRoleCode);

        var series = requestValidatedMeasurementsMessageBase.Series
            .Cast<RequestValidatedMeasurementsSeries>()
            .Select(
                series => new InitializeRequestValidatedMeasurementsProcessSeries(
                    Id: TransactionId.From(series.TransactionId),
                    StartDateTime: series.StartDateTime,
                    EndDateTime: series.EndDateTime,
                    MeteringPointId: series.MeteringPointLocationId,
                    RequestedByActor: RequestedByActor.From(
                        senderActorNumber,
                        series.RequestedByActorRole ?? senderActorRole),
                    OriginalActor: OriginalActor.From(
                        series.OriginalActorNumber ?? senderActorNumber,
                        senderActorRole)))
            .ToList().AsReadOnly();

        return new InitializeRequestValidatedMeasurementsProcessDto(
            BusinessReason: requestValidatedMeasurementsMessageBase.BusinessReason,
            MessageId: requestValidatedMeasurementsMessageBase.MessageId,
            Series: series);
    }
}
