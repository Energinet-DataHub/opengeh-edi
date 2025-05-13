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

/// <summary>
/// Carries the data, which will result in a series element of a RSM-018 message.
/// </summary>
/// <param name="TransactionId">The Id which the series has.</param>
/// <param name="MeteringPointId">The metering point Id which are missing measurements on the given <paramref name="Date"/>. </param>
/// <param name="Date">The date which the <paramref name="MeteringPointId"/> are missing measurements for.</param>
public sealed record MissingMeasurementSeries(
    TransactionId TransactionId,
    MeteringPointId MeteringPointId,
    Instant Date);
