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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using NodaTime;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.Tests.Factories;

public static class AcceptedEnergyResultMessageDtoBuilder
{
    private const long CalculationResultVersion = 1;
    private const string GridAreaCode = "804";
    private static readonly ActorNumber _receiverNumber = ActorNumber.Create("1234567890123");
    private static readonly ActorRole _receiverRole = ActorRole.MeteredDataResponsible;
    private static readonly Guid _processId = Guid.NewGuid();
    private static readonly string _meteringPointType = MeteringPointType.Consumption.Code;
    private static readonly string? _settlementMethod = SettlementMethod.Flex.Code;
    private static readonly string _measureUnitType = MeasurementUnit.Kwh.Code;
    private static readonly string _resolution = Resolution.QuarterHourly.Code;
    private static readonly ActorNumber? _energySupplierNumber = ActorNumber.Create("1234567890123");
    private static readonly ActorNumber? _balanceResponsibleNumber = ActorNumber.Create("1234567890124");
    private static readonly Period _period = new(
        Instant.FromUtc(2024, 02, 02, 02, 02, 02),
        Instant.FromUtc(2024, 02, 02, 02, 02, 02));

    private static readonly IReadOnlyCollection<AcceptedEnergyResultMessagePoint> _points =
        new List<AcceptedEnergyResultMessagePoint>()
        {
            new(
                1,
                2,
                CalculatedQuantityQuality.Calculated,
                Instant.FromUtc(2024, 02, 02, 02, 02, 02).ToString()),
        };

    private static readonly string _businessReasonName = BusinessReason.BalanceFixing.Code;
    private static readonly string? _originalTransactionIdReference = Guid.NewGuid().ToString();
    private static readonly string? _settlementVersion = SettlementVersion.FirstCorrection.Code;
    private static readonly MessageId? _relatedToMessageId = MessageId.New();
    private static readonly EventId _eventId = EventId.From(Guid.NewGuid());

    public static AcceptedEnergyResultMessageDto Build()
    {
        return AcceptedEnergyResultMessageDto.Create(
            _receiverNumber,
            _receiverRole,
            _processId,
            _eventId,
            GridAreaCode,
            _meteringPointType,
            _settlementMethod,
            _measureUnitType,
            _resolution,
            _energySupplierNumber?.Value,
            _balanceResponsibleNumber?.Value,
            _period,
            _points,
            _businessReasonName,
            CalculationResultVersion,
            _originalTransactionIdReference,
            _settlementVersion,
            _relatedToMessageId);
    }
}
