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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;

public record SendMeasurementsDto(
    MessageId MessageId,
    TransactionId TransactionId,
    Actor Sender,
    DateTimeOffset CreatedAt,
    Resolution Resolution,
    DateTimeOffset Start,
    DateTimeOffset End,
    MeteringPointId MeteringPointId,
    MeteringPointType MeteringPointType,
    IReadOnlyCollection<SendMeasurementsDto.MeasurementDto> Measurements)
{
    public record MeasurementDto(
        int Position,
        decimal Quantity,
        Quality Quality);
}
