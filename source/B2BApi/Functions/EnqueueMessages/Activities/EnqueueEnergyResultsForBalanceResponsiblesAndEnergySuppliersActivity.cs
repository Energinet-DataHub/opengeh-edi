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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

/// <summary>
/// Enqueue energy results for Balance Responsibles and Energy Suppliers as outgoing messages for the given calculation id.
/// </summary>
public class EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity(
    ILogger<EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity> logger,
    IServiceScopeFactory serviceScopeFactory,
    EnergyResultEnumerator energyResultEnumerator,
    DateTimeZone dateTimeZone)
    : EnqueueEnergyResultsBaseActivity(logger, serviceScopeFactory, energyResultEnumerator)
{
    private readonly ILogger<EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity> _logger = logger;
    private readonly EnergyResultEnumerator _energyResultEnumerator = energyResultEnumerator;
    private readonly DateTimeZone _dateTimeZone = dateTimeZone;

    [Function(nameof(EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity))]
    public Task<int> Run(
        [ActivityTrigger] EnqueueMessagesInput input)
    {
        var query = new EnergyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaQuery(
            _logger,
            _energyResultEnumerator.EdiDatabricksOptions,
            EventId.From(input.EventId),
            input.CalculationId,
            _dateTimeZone);

        return EnqueueEnergyResults(input, query);
    }

    protected override Task EnqueueAndCommitEnergyResult<TOutgoingMessage>(IOutgoingMessagesClient outgoingMessagesClient, TOutgoingMessage outgoingMessageDto)
    {
        return outgoingMessageDto is EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDto perEnergySupplierPerBalanceResponsibleMessageDto
            ? outgoingMessagesClient.EnqueueAndCommitAsync(perEnergySupplierPerBalanceResponsibleMessageDto, CancellationToken.None)
            : throw new ArgumentException(
                $"The outgoing message dto is not of the expected type {typeof(EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDto).FullName}",
                nameof(outgoingMessageDto));
    }
}
