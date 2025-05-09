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
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MissingMeasurementMessages;

// TODO: The process manager contract has a list of dates with a metering point
// The schema for RSM-018 has a list of metering points for a date.
// How do we solve this?
public sealed record MissingMeasurementSeries(
    TransactionId TransactionId,
    MeteringPointId MeteringPointIds,
    Instant? RegistrationDateTime);
