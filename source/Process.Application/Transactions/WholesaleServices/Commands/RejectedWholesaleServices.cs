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
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Domain.Commands;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Commands;

public sealed class RejectedWholesaleServices : InternalCommand
{
    [JsonConstructor]
    public RejectedWholesaleServices(EventId eventId, Guid processId, IReadOnlyCollection<RejectReasonDto> rejectReasons)
    {
        EventId = eventId;
        ProcessId = processId;
        RejectReasons = rejectReasons;
    }

    public EventId EventId { get; }

    public Guid ProcessId { get; }

    public IReadOnlyCollection<RejectReasonDto> RejectReasons { get; }
}
