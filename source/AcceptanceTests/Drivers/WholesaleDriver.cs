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
    public const string BalanceResponsiblePartyMarketRoleCode = "DDK";
    private readonly IntegrationEventPublisher _integrationEventPublisher;

    internal WholesaleDriver(IntegrationEventPublisher integrationEventPublisher)
    {
        _integrationEventPublisher = integrationEventPublisher;
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
            aggregation.ToByteArray());
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
            monthlyAmountPerChargeResultProduced.ToByteArray());
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
            amountPerChargeResultProduced.ToByteArray());
    }
}
