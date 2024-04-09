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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.MasterData.Interfaces;

namespace Energinet.DataHub.EDI.IncomingMessages.Application;

public class IncomingMessageDelegator
{
    private static readonly HashSet<ActorRole> _rolesWithAllowedDelegation = new()
    {
        ActorRole.GridOperator,
        ActorRole.Delegated,
    };

    private readonly IMasterDataClient _masterDataClient;
    private readonly AuthenticatedActor _authenticatedActor;

    public IncomingMessageDelegator(IMasterDataClient masterDataClient, AuthenticatedActor authenticatedActor)
    {
        _masterDataClient = masterDataClient;
        _authenticatedActor = authenticatedActor;
    }

    public async Task DelegateAsync(
        IIncomingMessage message,
        IncomingDocumentType documentType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var processType = MapToProcessType(documentType);

        var senderNumber = ActorNumber.TryCreate(message.SenderNumber);
        var currentActorRole = _authenticatedActor.CurrentActorIdentity.MarketRole != null // TODO: What is the role in ProcessDelegation? Is it DEL or is it the "target role"?
            ? ActorRole.TryFromCode(_authenticatedActor.CurrentActorIdentity.MarketRole.Code)
            : null;
        var senderRole = ActorRole.TryFromCode(message.SenderRoleCode);

        if (senderNumber is null || currentActorRole is null || senderRole is null)
        {
            // Since actor number or actor role was invalid, there can't be setup any process delegation, so do nothing
            return;
        }

        if (!_rolesWithAllowedDelegation.Contains(currentActorRole))
        {
            // Only grid operators and delegated actors can have delegations setup, so do nothing
            return;
        }

        // Delegation is setup for grid areas, so we need to set delegated for each series since they contain the grid area
        foreach (var series in message.Serie)
        {
            var delegations = await _masterDataClient.GetProcessesDelegatedToAsync(
                    senderNumber,
                    currentActorRole,
                    series.GridArea,
                    processType,
                    cancellationToken)
                .ConfigureAwait(false);

            if (delegations.Count != 0)
            {
                var byActorNumber = delegations.First().DelegatedBy.ActorNumber;
                var byActorRole = delegations.First().DelegatedBy.ActorRole;

                series.SetDelegated(senderRole, delegations.Select(d => d.GridAreaCode).ToArray());
            }
        }
    }

    private ProcessType MapToProcessType(IncomingDocumentType incomingDocumentType)
    {
        if (incomingDocumentType == IncomingDocumentType.RequestAggregatedMeasureData)
            return ProcessType.RequestEnergyResults;
        if (incomingDocumentType == IncomingDocumentType.RequestWholesaleSettlement)
            return ProcessType.RequestWholesaleResults;

        throw new ArgumentOutOfRangeException(nameof(incomingDocumentType), incomingDocumentType, $"Cannot map {nameof(IncomingDocumentType)} to {nameof(ProcessType)}");
    }
}
