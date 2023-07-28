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
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.Commands;
using Application.IncomingMessages.RequestAggregatedMeasureData;
using Application.Transactions.AggregatedMeasureData.Notifications;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using MediatR;

namespace Application.Transactions.AggregatedMeasureData;

public class AggregatedMeasureDataAcceptedHandler : IRequestHandler<AggregatedMeasureDataAccepted, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;
    private readonly ICommandScheduler _commandScheduler;

    public AggregatedMeasureDataAcceptedHandler(ICommandScheduler commandScheduler, IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository)
    {
        _commandScheduler = commandScheduler;
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
    }

    public async Task<Unit> Handle(AggregatedMeasureDataAccepted request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var process = _aggregatedMeasureDataProcessRepository.GetById(ProcessId.Create(request.ProcessId));
        ArgumentNullException.ThrowIfNull(process);

        if (process.HasWholesaleAlreadyReplied())
        {
            return Unit.Value;
        }

        process.CheckThatProcessReadyForWholesaleReply();
        var internalCommand = new AggregatedMeasureDataAcceptedInternalCommand(request);
        await _commandScheduler.EnqueueAsync(internalCommand).ConfigureAwait(false);
        process.ReplyFromWholesaleAccepted();
        return Unit.Value;
    }
}
