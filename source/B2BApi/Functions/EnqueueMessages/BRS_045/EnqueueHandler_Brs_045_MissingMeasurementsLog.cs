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

using System.Security.Cryptography;
using System.Text;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MissingMeasurementMessages;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_045.MissingMeasurementsLogCalculation.V1.Model;
using NodaTime.Extensions;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_045;

public class EnqueueHandler_Brs_045_MissingMeasurementsLog(
    IOutgoingMessagesClient outgoingMessagesClient,
    IUnitOfWork unitOfWork)
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task HandleAsync(
        EnqueueMissingMeasurementsLogHttpV1 missingMeasurementsLog,
        CancellationToken cancellationToken)
    {
        foreach (var message in missingMeasurementsLog.Data)
        {
            var outgoingMessageDto = CreateOutgoingMessageDto(missingMeasurementsLog.OrchestrationInstanceId, message);

            await _outgoingMessagesClient.EnqueueAsync(
                outgoingMessageDto,
                cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    private MissingMeasurementMessageDto CreateOutgoingMessageDto(
        Guid orchestrationInstanceId,
        EnqueueMissingMeasurementsLogHttpV1.DateWithMeteringPointId data)
    {
        return new MissingMeasurementMessageDto(
            eventId: EventId.From(Guid.CreateVersion7()),
            orchestrationInstanceId: orchestrationInstanceId,
            receiver: new Actor(
                ActorNumber.Create(data.GridAccessProvider.Value),
                ActorRole.MeteredDataResponsible),
            businessReason: BusinessReason.ReminderOfMissingMeasurementLog,
            gridAreaCode: data.GridArea,
            missingMeasurement: new MissingMeasurement(
                TransactionId: TransactionId.New(),
                MeteringPointId: MeteringPointId.From(data.MeteringPointId),
                Date: data.Date.ToInstant()));
    }
}
