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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Domain;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Interfaces;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;

public class InitializeWholesaleServicesProcessesHandler : IRequestHandler<InitializeWholesaleServicesProcessesCommand, Unit>
{
    private readonly IWholesaleServicesProcessRepository _wholesaleServicesProcessRepository;

    public InitializeWholesaleServicesProcessesHandler(IWholesaleServicesProcessRepository wholesaleServicesProcessRepository)
    {
        _wholesaleServicesProcessRepository = wholesaleServicesProcessRepository;
    }

    public Task<Unit> Handle(InitializeWholesaleServicesProcessesCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.InitializeWholesaleServicesProcessDto);

        CreateWholesaleServicesProcess(request.InitializeWholesaleServicesProcessDto);

        return Task.FromResult(Unit.Value);
    }

    private void CreateWholesaleServicesProcess(InitializeWholesaleServicesProcessDto initializeProcessDto)
    {
        var businessReason = BusinessReason.FromCodeOrUnused(initializeProcessDto.BusinessReason);
        var messageId = MessageId.Create(initializeProcessDto.MessageId);

        foreach (var series in initializeProcessDto.Series)
        {
            var settlementVersion = !string.IsNullOrWhiteSpace(series.SettlementVersion)
                ? SettlementVersion.FromCodeOrUnused(series.SettlementVersion)
                : null;

            var chargeTypes = series.ChargeTypes
                .Select(
                    chargeType => new Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices.ChargeType(
                        ChargeTypeId.New(),
                        chargeType.Id,
                        chargeType.Type))
                .ToList();

            _wholesaleServicesProcessRepository.Add(
                new WholesaleServicesProcess(
                    processId: ProcessId.New(),
                    series.RequestedByActor,
                    series.OriginalActor,
                    businessTransactionId: BusinessTransactionId.Create(series.Id),
                    initiatedByMessageId: messageId,
                    businessReason: businessReason,
                    startOfPeriod: series.StartDateTime,
                    endOfPeriod: series.EndDateTime,
                    requestedGridArea: series.RequestedGridAreaCode,
                    energySupplierId: series.EnergySupplierId,
                    settlementVersion: settlementVersion,
                    resolution: series.Resolution,
                    chargeOwner: series.ChargeOwner,
                    chargeTypes: chargeTypes,
                    gridAreas: series.GridAreas));
        }
    }
}
