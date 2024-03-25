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
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.EventProcessors;

public class ProcessDelegationConfiguredEventProcessor : IIntegrationEventProcessor
{
    private readonly IMasterDataClient _masterDataClient;

    public ProcessDelegationConfiguredEventProcessor(IMasterDataClient masterDataClient)
    {
        _masterDataClient = masterDataClient;
    }

    public string EventTypeToHandle => ProcessDelegationConfigured.EventName;

    public async Task ProcessAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var message = (ProcessDelegationConfigured)integrationEvent.Message;
        await _masterDataClient.CreateProcessDelegationAsync(
            new ProcessDelegationDto(
                message.SequenceNumber,
                MapToProcessType(message.Process.ToString()),
                message.GridAreaCode,
                message.StartsAt.ToInstant(),
                message.StopsAt.ToInstant(),
                new ActorNumberAndRoleDto(ActorNumber.Create(message.DelegatedByActorNumber), EicFunctionMapper.GetMarketRole(message.DelegatedByActorRole)),
                new ActorNumberAndRoleDto(ActorNumber.Create(message.DelegatedToActorNumber), EicFunctionMapper.GetMarketRole(message.DelegatedToActorRole))),
            cancellationToken).ConfigureAwait(false);
    }

    private static ProcessType MapToProcessType(string delegatedProcess)
    {
        return delegatedProcess switch
        {
            "ProcessRequestEnergyResults" => ProcessType.RequestedEnergyResults,
            "ProcessReceiveEnergyResults" => ProcessType.RequestedEnergyResults,
            "ProcessRequestWholesaleResults" => ProcessType.RequestedWholesaleResults,
            "ProcessReceiveWholesaleResults" => ProcessType.CalculatedWholesaleResults,
            _ => throw new ArgumentOutOfRangeException(nameof(delegatedProcess), delegatedProcess, null),
        };
    }
}
