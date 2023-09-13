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
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Infrastructure.Wholesale;
using MediatR;

namespace Infrastructure.Transactions.AggregatedMeasureData.Commands.Handlers;

public class SendAggregatedMeasuredDataToWholesale
    : IRequestHandler<SendAggregatedMeasureRequestToWholesale, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;
    private readonly WholesaleInbox _wholesaleInbox;

    public SendAggregatedMeasuredDataToWholesale(
        IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository,
        WholesaleInbox wholesaleInbox)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
        _wholesaleInbox = wholesaleInbox;
    }

    public async Task<Unit> Handle(
        SendAggregatedMeasureRequestToWholesale request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var process = await _aggregatedMeasureDataProcessRepository
            .GetByIdAsync(ProcessId.Create(request.ProcessId), cancellationToken).ConfigureAwait(false);

        await _wholesaleInbox.SendAsync(
            process,
            cancellationToken).ConfigureAwait(false);

        process.WasSentToWholesale();

        return Unit.Value;
    }
}
