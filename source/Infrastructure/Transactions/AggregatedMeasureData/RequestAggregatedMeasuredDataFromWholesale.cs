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
using Application.Transactions.AggregatedMeasureData.Notifications;
using Application.Wholesale;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using MediatR;

namespace Infrastructure.Transactions.AggregatedMeasureData;

public class
    RequestAggregatedMeasuredDataFromWholesale : IRequestHandler<NotifyWholesaleOfAggregatedMeasureDataRequest, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;
    private readonly IWholesaleInbox _wholesaleInbox;

    public RequestAggregatedMeasuredDataFromWholesale(
        IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository,
        IWholesaleInbox wholesaleInbox)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
        _wholesaleInbox = wholesaleInbox;
    }

    public async Task<Unit> Handle(
        NotifyWholesaleOfAggregatedMeasureDataRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var process = _aggregatedMeasureDataProcessRepository.GetById(ProcessId.Create(request.ProcessId)) ??
                      throw new ArgumentNullException(nameof(request));

        await _wholesaleInbox.SendAsync(
            process,
            cancellationToken).ConfigureAwait(false);
        process.WholesaleIsNotifiedOfRequest();
        return Unit.Value;
    }
}
