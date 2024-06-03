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
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<IncomingMessageDelegator> _logger;

    public IncomingMessageDelegator(IMasterDataClient masterDataClient, AuthenticatedActor authenticatedActor, ILogger<IncomingMessageDelegator> logger)
    {
        _masterDataClient = masterDataClient;
        _authenticatedActor = authenticatedActor;
        _logger = logger;
    }

    public async Task DelegateAsync(
        IIncomingMessage message,
        IncomingDocumentType documentType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var processType = MapToProcessType(documentType);

        // Incoming message has current actor as sender, but uses the role of the original actor. This means:
        // - message.SenderNumber is the delegated TO actor number (requested by actor number)
        // - AuthenticatedActor has the delegated TO actor role (requested by actor role)
        // - message.SenderRoleCode is the delegated BY actor role (original actor role)
        // - the original actor number is found in the series based on the original actor role
        var requestedByActorNumber = ActorNumber.TryCreate(message.SenderNumber);
        var requestedByActorRole = _authenticatedActor.CurrentActorIdentity.MarketRole != null
            ? ActorRole.TryFromCode(_authenticatedActor.CurrentActorIdentity.MarketRole.Code)
            : null;

        var originalActorRole = ActorRole.TryFromCode(message.SenderRoleCode);

        if (requestedByActorNumber is null || requestedByActorRole is null || originalActorRole is null)
        {
            // Since actor number or actor role was invalid, there can't be setup any process delegation, so do nothing
            return;
        }

        if (!_rolesWithAllowedDelegation.Contains(requestedByActorRole))
        {
            // Only grid operators and delegated actors can have delegations setup, so do nothing
            return;
        }

        // Delegation is setup for grid areas, so we need to set delegated for each series since they contain the grid area
        foreach (var series in message.Series)
        {
            if ((originalActorRole == ActorRole.GridOperator || originalActorRole == ActorRole.MeteredDataResponsible)
                && series.GridArea == null)
            {
                // TODO: How do we handle this case for MDR/DDM?
                // If you request with gridarea == null, then we can't find the grid area owner. This means delegation
                // will be skipped, but this will cause the sync validation the actornumber in the message
                // is not the same as the logged in actor. This means that Wholesale's async validation for grid area
                // will never be reached, since the message is not valid. Maybe it's okay that they just get a sync
                // validation error (unauthorized sender id), since their request is invalid.
                continue;
            }

            var delegatedTo = new Actor(requestedByActorNumber, requestedByActorRole);
            var delegations = await _masterDataClient.GetProcessesDelegatedToAsync(
                    delegatedTo,
                    series.GridArea,
                    processType,
                    cancellationToken)
                .ConfigureAwait(false);

            if (delegations.Count != 0)
            {
                ActorNumber? gridAreaOwner = null;
                if (series.GridArea != null)
                {
                    // Try to get grid area owner for the grid area, returning null if none was found,
                    // we cannot fail if no owner was found, since validation hasn't been done yet
                    gridAreaOwner = await _masterDataClient.TryGetGridOwnerForGridAreaCodeAsync(
                            series.GridArea,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                var originalActorNumber = series.GetActorNumberForRole(originalActorRole, gridAreaOwner);
                if (originalActorNumber == null)
                {
                    // Some part of the incoming message is invalid, since we cannot find the original
                    // actor number, so do nothing
                    _logger.LogWarning(
                        "Cannot find original actor number for role {Role} in incoming message {DocumentType} (message id: {MessageId})",
                        originalActorRole,
                        documentType,
                        message.MessageId);
                    return;
                }

                var delegationsForOriginalActor = delegations
                    .Where(d => d.DelegatedBy.ActorNumber == originalActorNumber)
                    .ToList();
                if (delegationsForOriginalActor.Count == 0)
                {
                    _logger.LogInformation(
                        "Cannot find delegation relation ship between delegatedBy {DelegatedBy} and delegatedTo {DelegatedTo} on grid area {GridArea} for process {ProcessType} in incoming message {DocumentType} (message id: {MessageId}",
                        new Actor(originalActorNumber, originalActorRole).ToString(),
                        delegatedTo.ToString(),
                        series.GridArea,
                        processType,
                        documentType,
                        message.MessageId);

                    // The original actor number is not delegated to the requested by actor, so do nothing
                    return;
                }

                series.DelegateSeries(originalActorNumber, requestedByActorRole, delegationsForOriginalActor.Select(d => d.GridAreaCode).ToArray());
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
