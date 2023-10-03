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
using Energinet.DataHub.EDI.Application.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Transactions;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using MediatR;

namespace Energinet.DataHub.EDI.Application.Transactions.Aggregations;

public class ForwardAggregationResultHandler : IRequestHandler<ForwardAggregationResult, Unit>
{
    private readonly IAggregationResultForwardingRepository _aggregationResultForwardingRepository;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;

    public ForwardAggregationResultHandler(IAggregationResultForwardingRepository aggregationResultForwardingRepository, IOutgoingMessageRepository outgoingMessageRepository)
    {
        _aggregationResultForwardingRepository = aggregationResultForwardingRepository;
        _outgoingMessageRepository = outgoingMessageRepository;
    }

    public Task<Unit> Handle(ForwardAggregationResult request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var aggregationResultForwarding = new AggregationResultForwarding(ProcessId.New());
        _aggregationResultForwardingRepository.Add(aggregationResultForwarding);
        _outgoingMessageRepository.Add(aggregationResultForwarding.CreateMessage(request.Result));
        return Unit.Task;
    }
}
