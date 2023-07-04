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
using Application.WholeSale;
using Domain.Transactions.AggregatedMeasureData;
using MediatR;

namespace Application.Transactions.AggregatedMeasureData;

public class RequestAggregatedMeasuredDataFromWholesale : IRequestHandler<NotifyWholesaleOfAggregatedMeasureDataRequest, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;
    private readonly IWholeSaleInBox<AggregatedMeasureDataProcess> _wholeSaleInBox;

    public RequestAggregatedMeasuredDataFromWholesale(
        IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository,
        IWholeSaleInBox<AggregatedMeasureDataProcess> wholeSaleInBox)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
        _wholeSaleInBox = wholeSaleInBox;
    }

    public async Task<Unit> Handle(
        NotifyWholesaleOfAggregatedMeasureDataRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var process = _aggregatedMeasureDataProcessRepository.GetById(request.ProcessId) ?? throw new ArgumentNullException(nameof(request));

        // send message
        await _wholeSaleInBox.SendAsync(CreateWholeSaleMessage(), cancellationToken).ConfigureAwait(false);
        process.WholesaleIsNotifiedOfRequest();
        return Unit.Value;
    }

    // LRN: consider move this to a factory, it could return a AggregatedMeasureDataAcceptedResponse or something like that.
    private static AggregatedMeasureDataProcess CreateWholeSaleMessage()
    {
        throw new NotImplementedException();
        //return new AggregatedMeasureDataProcess();
    }
}
