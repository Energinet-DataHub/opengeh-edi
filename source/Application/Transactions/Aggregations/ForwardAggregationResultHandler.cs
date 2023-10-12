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
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using MediatR;

namespace Energinet.DataHub.EDI.Application.Transactions.Aggregations;

public class ForwardAggregationResultHandler : IRequestHandler<ForwardAggregationResult, Unit>
{
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly IOutgoingMessagesConfigurationRepository _outgoingMessagesConfigurationRepository;
    private readonly DocumentFactory _documentFactory;

    public ForwardAggregationResultHandler(IOutgoingMessageRepository outgoingMessageRepository, IOutgoingMessagesConfigurationRepository outgoingMessagesConfigurationRepository, DocumentFactory documentFactory)
    {
        _outgoingMessageRepository = outgoingMessageRepository;
        _outgoingMessagesConfigurationRepository = outgoingMessagesConfigurationRepository;
        _documentFactory = documentFactory;
    }

    public async Task<Unit> Handle(ForwardAggregationResult request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var documentFormat = DocumentFormat.Xml;

        if (AggregationResultMessageFactory.IsTotalResultPerGridArea(request.Result))
        {
            documentFormat = await _outgoingMessagesConfigurationRepository.GetDocumentFormatAsync(ActorNumber.Create(request.Result.GridAreaDetails!.OperatorNumber), MarketRole.MeteredDataResponsible, DocumentType.NotifyAggregatedMeasureData).ConfigureAwait(false);
        }
        else if (AggregationResultMessageFactory.ResultIsForTheEnergySupplier(request.Result))
        {
            documentFormat = await _outgoingMessagesConfigurationRepository.GetDocumentFormatAsync(ActorNumber.Create(request.Result.ActorGrouping!.EnergySupplierNumber!), MarketRole.EnergySupplier, DocumentType.NotifyAggregatedMeasureData).ConfigureAwait(false);
        }
        else if (AggregationResultMessageFactory.ResultIsForTheBalanceResponsible(request.Result))
        {
            documentFormat = await _outgoingMessagesConfigurationRepository.GetDocumentFormatAsync(ActorNumber.Create(request.Result.ActorGrouping!.BalanceResponsibleNumber!), MarketRole.EnergySupplier, DocumentType.NotifyAggregatedMeasureData).ConfigureAwait(false);
        }

        var documentWriter = _documentFactory.GetWriter(DocumentType.NotifyAggregatedMeasureData, documentFormat);
        _outgoingMessageRepository.Add(AggregationResultMessageFactory.CreateMessage(request.Result, ProcessId.New(), documentWriter));
        return Unit.Value;
    }
}
