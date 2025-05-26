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
    public static InitializeRequestMeasurementsProcessDto Create(RequestMeasurementsMessageBase requestMeasurementsMessageBase)
    {
        ArgumentNullException.ThrowIfNull(requestMeasurementsMessageBase);

        var senderActorNumber = ActorNumber.Create(requestMeasurementsMessageBase.SenderNumber);
        var senderActorRole = ActorRole.FromCode(requestMeasurementsMessageBase.SenderRoleCode);

        var series = requestMeasurementsMessageBase.Series
            .Cast<RequestValidatedMeasurementsSeries>()
            .Select(
                series => new InitializeRequestMeasurementsProcessSeries(
                    Id: TransactionId.From(series.TransactionId),
                    StartDateTime: series.StartDateTime,
                    EndDateTime: series.EndDateTime,
                    MeteringPointId: series.MeteringPointId,
                    OriginalActor: OriginalActor.From(
                        senderActorNumber,
                        senderActorRole)))
            .ToList().AsReadOnly();

        return new InitializeRequestMeasurementsProcessDto(
            BusinessReason: requestMeasurementsMessageBase.BusinessReason,
            MessageId: requestMeasurementsMessageBase.MessageId,
            Series: series);
    }
}
