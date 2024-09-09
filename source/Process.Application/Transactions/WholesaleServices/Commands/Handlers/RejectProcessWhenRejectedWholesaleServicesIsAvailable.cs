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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages.Request;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Commands.Handlers;

public sealed class
    RejectProcessWhenRejectedWholesaleServicesIsAvailable : IRequestHandler<RejectedWholesaleServices, Unit>
{
    private readonly IWholesaleServicesProcessRepository _wholesaleServicesProcessRepository;

    public RejectProcessWhenRejectedWholesaleServicesIsAvailable(
        IWholesaleServicesProcessRepository wholesaleServicesProcessRepository)
    {
        _wholesaleServicesProcessRepository = wholesaleServicesProcessRepository;
    }

    public async Task<Unit> Handle(RejectedWholesaleServices request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var process = await _wholesaleServicesProcessRepository
            .GetAsync(ProcessId.Create(request.ProcessId), cancellationToken)
            .ConfigureAwait(false);

        process.IsRejected(CreateRejectedWholesaleServicesResultMessage(request.EventId, process, request.RejectReasons));

        return Unit.Value;
    }

    private RejectedWholesaleServicesMessageDto CreateRejectedWholesaleServicesResultMessage(
        EventId eventId,
        WholesaleServicesProcess process,
        IReadOnlyCollection<RejectReasonDto> rejectReasons)
    {
        var rejectedWholesaleServices = new RejectedWholesaleServicesMessageSeries(
            TransactionId.New(),
            rejectReasons.Select(
                    reason =>
                        new RejectedWholesaleServicesMessageRejectReason(
                            reason.ErrorCode,
                            reason.ErrorMessage))
                .ToList(),
            process.BusinessTransactionId);

        return new RejectedWholesaleServicesMessageDto(
            receiverNumber: process.RequestedByActor.ActorNumber,
            processId: process.ProcessId.Id,
            eventId: eventId,
            businessReason: process.BusinessReason.Name,
            receiverRole: process.RequestedByActor.ActorRole,
            relatedToMessageId: process.InitiatedByMessageId,
            series: rejectedWholesaleServices,
            documentReceiverNumber: process.OriginalActor.ActorNumber,
            documentReceiverRole: process.OriginalActor.ActorRole);
    }
}
