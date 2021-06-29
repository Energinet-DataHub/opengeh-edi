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
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands
{
    public class QueuedInternalCommand
    {
        public QueuedInternalCommand(Guid id, string type, byte[] data, Instant creationDate, Guid businessProcessId, Instant? scheduleDate)
        {
            Id = id;
            Type = type;
            Data = data;
            CreationDate = creationDate;
            ScheduleDate = scheduleDate;
            BusinessProcessId = businessProcessId;
        }

        public Guid Id { get; }

        public string Type { get;  } = string.Empty;

        #pragma warning disable CA1819 // Properties should not return arrays
        public byte[] Data { get; }

        public Instant CreationDate { get; private set; }

        public Instant? ScheduleDate { get; private set; }

        public Instant? ProcessedDate { get; set; }

        public Guid BusinessProcessId { get; private set; }

        public Instant? DispatchedDate { get; private set; }

        public void SetProcessed(Instant now)
        {
            ProcessedDate = now;
        }

        public void SetDispatched(Instant now)
        {
            DispatchedDate = now;
        }
    }
}
