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
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.Transactions;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Infrastructure.Transactions.Aggregations;
using MediatR;

namespace Energinet.DataHub.EDI.Infrastructure.Transactions.AggregatedMeasureData.Commands.Handlers;

public class AcceptProcessWhenAcceptedAggregatedTimeSeriesIsAvailable : IRequestHandler<AcceptedAggregatedTimeSerie, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;
    private readonly IOutgoingMessagesConfigurationRepository _outgoingMessagesConfigurationRepository;
    private readonly DocumentFactory _documentFactory;

    public AcceptProcessWhenAcceptedAggregatedTimeSeriesIsAvailable(
        IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository,
        IOutgoingMessagesConfigurationRepository outgoingMessagesConfigurationRepository,
        DocumentFactory documentFactory)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
        _outgoingMessagesConfigurationRepository = outgoingMessagesConfigurationRepository;
        _documentFactory = documentFactory;
    }

    public async Task<Unit> Handle(AcceptedAggregatedTimeSerie request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var process = await _aggregatedMeasureDataProcessRepository
            .GetAsync(ProcessId.Create(request.ProcessId), cancellationToken).ConfigureAwait(false);

        var aggregation = AggregationFactory.Create(process, request.AggregatedTimeSerie);
        var documentFormat = await _outgoingMessagesConfigurationRepository.GetDocumentFormatAsync(process.RequestedByActorId, MarketRole.FromCode(process.RequestedByActorRoleCode), DocumentType.NotifyAggregatedMeasureData).ConfigureAwait(false);

        var documentWriter = _documentFactory.GetWriter(DocumentType.NotifyAggregatedMeasureData, documentFormat);

        process.IsAccepted(aggregation, documentWriter);

        return Unit.Value;
    }
}
