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

using Energinet.DataHub.EDI.AcceptanceTests.Drivers.MessageFactories;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Google.Protobuf;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

internal sealed class WholesaleDriver
{
    private const string BalanceResponsiblePartyMarketRoleCode = "DDK";
    private readonly IntegrationEventPublisher _integrationEventPublisher;
    private readonly EdiInboxClient _inboxEdiClient;

    internal WholesaleDriver(
        IntegrationEventPublisher integrationEventPublisher,
        EdiInboxClient inboxEdiClient)
    {
        _integrationEventPublisher = integrationEventPublisher;
        _inboxEdiClient = inboxEdiClient;
    }

    internal Task PublishAggregationResultAsync(string gridAreaCode, ActorRole? marketRole = null, string? actorNumber = null)
    {
        var aggregation = marketRole?.Code switch
        {
            BalanceResponsiblePartyMarketRoleCode => EnergyResultProducedV2Factory
                .CreateAggregationResultAvailableEventForBalanceResponsible(
                    gridAreaCode,
                    actorNumber ?? throw new ArgumentNullException(nameof(actorNumber))),
            _ => EnergyResultProducedV2Factory.CreateAggregationResultAvailableEventFor(gridAreaCode),
        };

        return _integrationEventPublisher.PublishAsync(
            EnergyResultProducedV2.EventName,
            aggregation.ToByteArray(),
            waitForHandled: true);
    }

    internal Task PublishMonthlyAmountPerChargeResultAsync(
        string gridAreaCode,
        string energySupplierId,
        string chargeOwnerId)
    {
        var monthlyAmountPerChargeResultProduced =
            MonthlyAmountPerChargeResultProducedV1Factory.CreateMonthlyAmountPerChargeResultProduced(
                gridAreaCode,
                energySupplierId,
                chargeOwnerId);

        return _integrationEventPublisher.PublishAsync(
            MonthlyAmountPerChargeResultProducedV1.EventName,
            monthlyAmountPerChargeResultProduced.ToByteArray(),
            waitForHandled: true);
    }

    internal Task PublishAmountPerChargeResultAsync(
        string gridAreaCode,
        string energySupplierId,
        string chargeOwnerId)
    {
        var amountPerChargeResultProduced =
            AmountPerChargeResultProducedV1Factory.CreateAmountPerChargeResultProduced(
                gridAreaCode,
                energySupplierId,
                chargeOwnerId);

        return _integrationEventPublisher.PublishAsync(
            AmountPerChargeResultProducedV1.EventName,
            amountPerChargeResultProduced.ToByteArray(),
            waitForHandled: true);
    }

    internal async Task PublishWholesaleServicesRequestAcceptedResponseAsync(
        Guid processId,
        string gridAreaCode,
        string energySupplierNumber,
        string chargeOwnerNumber,
        CancellationToken cancellationToken)
    {
        var message = WholesaleServiceRequestAcceptedMessageFactory.Create(
            processId,
            gridAreaCode,
            energySupplierNumber,
            chargeOwnerNumber);

        await _inboxEdiClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    internal async Task PublishWholesaleServicesRequestRejectedResponseAsync(Guid processId, CancellationToken cancellationToken)
    {
        var message = WholesaleServiceRequestRejectedMessageFactory.Create(processId);

        await _inboxEdiClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    internal async Task PublishAggregatedMeasureDataRequestAcceptedResponseAsync(
        Guid processId,
        string gridAreaCode,
        CancellationToken cancellationToken)
    {
        var message = AggregatedMeasureDataRequestAcceptedMessageFactory.Create(
            processId,
            gridAreaCode);

        await _inboxEdiClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    internal async Task PublishAggregatedMeasureDataRequestRejectedResponseAsync(Guid processId, CancellationToken cancellationToken)
    {
        var message = AggregatedMeasureDataRequestRejectedMessageFactory.Create(processId);

        await _inboxEdiClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    internal Task PublishCalculationCompletedAsync(
        Guid calculationId,
        CalculationCompletedV1.Types.CalculationType calculationType)
    {
        var calculationCompleted = CalculationCompletedV1Factory.CreateCalculationCompleted(
            calculationId,
            calculationType);

        return _integrationEventPublisher.PublishAsync(
            CalculationCompletedV1.EventName,
            calculationCompleted.ToByteArray(),
            waitForHandled: true);
    }
}
