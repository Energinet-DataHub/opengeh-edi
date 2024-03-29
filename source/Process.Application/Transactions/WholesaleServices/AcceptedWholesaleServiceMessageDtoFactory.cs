﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Mappers;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;

public static class AcceptedWholesaleServiceMessageDtoFactory
{
    public static AcceptedWholesaleServicesMessageDto Create(
        WholesaleServicesProcess process,
        AcceptedWholesaleServicesSerieDto acceptedWholesaleServices)
    {
        ArgumentNullException.ThrowIfNull(process);
        ArgumentNullException.ThrowIfNull(acceptedWholesaleServices);

        var message = CreateWholesaleResultSeries(process, acceptedWholesaleServices);

        return AcceptedWholesaleServicesMessageDto.Create(
            process.RequestedByActorId,
            ActorRole.FromCode(process.RequestedByActorRoleCode),
            message.ChargeOwner,
            process.ProcessId.Id,
            process.BusinessReason.Name,
            message,
            process.InitiatedByMessageId);
    }

    private static AcceptedWholesaleServicesSeries CreateWholesaleResultSeries(
        WholesaleServicesProcess process,
        AcceptedWholesaleServicesSerieDto acceptedWholesaleServices)
    {
        var acceptedWholesaleCalculationSeries = new AcceptedWholesaleServicesSeries(
            TransactionId: process.ProcessId.Id,
            CalculationVersion: acceptedWholesaleServices.CalculationResultVersion,
            GridAreaCode: acceptedWholesaleServices.GridArea,
            ChargeCode: acceptedWholesaleServices.ChargeCode,
            IsTax: false,
            Points: PointsMapper.MapPoints(acceptedWholesaleServices.Points),
            EnergySupplier: acceptedWholesaleServices.EnergySupplierId,
            ChargeOwner: acceptedWholesaleServices.ChargeOwnerId,
            Period: new Period(acceptedWholesaleServices.StartOfPeriod, acceptedWholesaleServices.EndOfPeriod),
            acceptedWholesaleServices.SettlementVersion,
            acceptedWholesaleServices.MeasurementUnit,
            PriceMeasureUnit: MeasurementUnit.Kwh,
            acceptedWholesaleServices.Currency,
            acceptedWholesaleServices.ChargeType,
            acceptedWholesaleServices.Resolution,
            acceptedWholesaleServices.MeteringPointType,
            acceptedWholesaleServices.SettlementType,
            OriginalTransactionIdReference: process.BusinessTransactionId.Id);

        return acceptedWholesaleCalculationSeries;
    }
}
