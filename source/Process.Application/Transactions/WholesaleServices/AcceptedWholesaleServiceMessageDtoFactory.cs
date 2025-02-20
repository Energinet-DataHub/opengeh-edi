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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages.Request;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Mappers;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;

public static class AcceptedWholesaleServiceMessageDtoFactory
{
    public static AcceptedWholesaleServicesMessageDto Create(
        EventId eventId,
        WholesaleServicesProcess process,
        AcceptedWholesaleServicesSerieDto acceptedWholesaleServices)
    {
        ArgumentNullException.ThrowIfNull(process);
        ArgumentNullException.ThrowIfNull(acceptedWholesaleServices);

        var message = CreateWholesaleResultSeries(process, acceptedWholesaleServices);

        return AcceptedWholesaleServicesMessageDto.Create(
            receiverNumber: process.RequestedByActor.ActorNumber,
            receiverRole: process.RequestedByActor.ActorRole,
            documentReceiverNumber: process.OriginalActor.ActorNumber,
            documentReceiverRole: process.OriginalActor.ActorRole,
            chargeOwnerId: acceptedWholesaleServices.ChargeOwnerId,
            processId: process.ProcessId.Id,
            eventId: eventId,
            businessReason: process.BusinessReason.Name,
            wholesaleSeries: message,
            relatedToMessageId: process.InitiatedByMessageId);
    }

    private static AcceptedWholesaleServicesSeries CreateWholesaleResultSeries(
        WholesaleServicesProcess process,
        AcceptedWholesaleServicesSerieDto wholesaleServices)
    {
        var acceptedWholesaleCalculationSeries = new AcceptedWholesaleServicesSeries(
            TransactionId: TransactionId.New(),
            CalculationVersion: wholesaleServices.CalculationResultVersion,
            GridAreaCode: wholesaleServices.GridArea,
            ChargeCode: wholesaleServices.ChargeCode,
            IsTax: false,
            Points: PointsMapper.Map(wholesaleServices.Points),
            EnergySupplier: wholesaleServices.EnergySupplierId,
            ChargeOwner: wholesaleServices.ChargeOwnerId,
            Period: new Period(wholesaleServices.StartOfPeriod, wholesaleServices.EndOfPeriod),
            wholesaleServices.SettlementVersion,
            wholesaleServices.MeasurementUnit,
            PriceMeasureUnit: IsTotalSum(wholesaleServices) ? null : MeasurementUnit.TryFromChargeType(wholesaleServices.ChargeType),
            wholesaleServices.Currency,
            wholesaleServices.ChargeType,
            wholesaleServices.Resolution,
            wholesaleServices.MeteringPointType,
            wholesaleServices.SettlementMethod,
            OriginalTransactionIdReference: process.BusinessTransactionId);

        return acceptedWholesaleCalculationSeries;
    }

    private static bool IsTotalSum(AcceptedWholesaleServicesSerieDto acceptedWholesaleServices)
    {
        return acceptedWholesaleServices.Points.Count == 1
               && acceptedWholesaleServices.Points.First().Price == null
               && acceptedWholesaleServices.Resolution == Resolution.Monthly;
    }
}
