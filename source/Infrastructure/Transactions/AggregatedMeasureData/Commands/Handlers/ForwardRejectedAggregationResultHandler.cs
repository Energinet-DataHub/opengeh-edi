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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.OutgoingMessages;
using Domain.Actors;
using Domain.OutgoingMessages.RejectedRequestAggregatedMeasureData;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Infrastructure.Configuration.Serialization;
using Infrastructure.OutgoingMessages.Common;
using MediatR;
using RejectReason = Domain.Transactions.AggregatedMeasureData.RejectReason;

namespace Infrastructure.Transactions.AggregatedMeasureData.Commands.Handlers;

[Obsolete("This can be delete when all ForwardRejectedAggregationResult commands has been processed.")]
public class ForwardRejectedAggregationResultHandler : IRequestHandler<ForwardRejectedAggregationResult, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly ISerializer _serializer;

    public ForwardRejectedAggregationResultHandler(
        IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository,
        IOutgoingMessageRepository outgoingMessageRepository,
        ISerializer serializer)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
        _outgoingMessageRepository = outgoingMessageRepository;
        _serializer = serializer;
    }

    public async Task<Unit> Handle(ForwardRejectedAggregationResult request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var process = await _aggregatedMeasureDataProcessRepository.GetByIdAsync(
            ProcessId.Create(request.ProcessId),
            cancellationToken).ConfigureAwait(false);

        var rejectedReasons = _serializer.Deserialize<IReadOnlyList<RejectReason>>(process.ResponseData ?? string.Empty);

        _outgoingMessageRepository.Add(CreateRejectedAggregationResultMessage(
            process,
            rejectedReasons));

        return Unit.Value;
    }

    private static RejectedAggregationResultMessage CreateRejectedAggregationResultMessage(
        AggregatedMeasureDataProcess process,
        IReadOnlyList<RejectReason> rejectedReasons)
    {
        var transactionId = TransactionId.Create(process.ProcessId.Id);
        var rejectedTimeSerie = new RejectedTimeSerie(
                transactionId.Id,
                rejectedReasons.Select(reason =>
                        new Domain.OutgoingMessages.RejectedRequestAggregatedMeasureData.RejectReason(
                            reason.ErrorCode,
                            reason.ErrorMessage))
                    .ToList(),
                process.BusinessTransactionId.Id);

        return new RejectedAggregationResultMessage(
            process.RequestedByActorId,
            transactionId,
            CimCode.To(process.BusinessReason).Name,
            MarketRole.FromCode(process.RequestedByActorRoleCode),
            rejectedTimeSerie);
    }
}
