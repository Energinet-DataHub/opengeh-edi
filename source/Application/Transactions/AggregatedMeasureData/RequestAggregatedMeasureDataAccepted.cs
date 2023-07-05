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
using Application.IncomingMessages.RequestAggregatedMeasureData;
using Domain.Transactions.AggregatedMeasureData;
using MediatR;

namespace Application.Transactions.AggregatedMeasureData;

public class RequestAggregatedMeasureDataAccepted : IRequestHandler<AggregatedMeasureDataAccepted, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;

    public RequestAggregatedMeasureDataAccepted(IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
    }

    public Task<Unit> Handle(AggregatedMeasureDataAccepted request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        //ArgumentNullException.ThrowIfNull(request);

        // var process = _aggregatedMeasureDataProcessRepository.GetById(request.ProcessId) ??
        //               throw new ArgumentNullException(nameof(request));

        //return Unit.Value;
    }
}
