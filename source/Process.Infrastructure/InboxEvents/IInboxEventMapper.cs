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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;

/// <summary>
/// Maps from an inbox event to a notification
/// </summary>
public interface IInboxEventMapper
{
    /// <summary>
    /// Map payload to a notification
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="eventId"></param>
    /// <param name="referenceId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="INotification"/></returns>
    Task<INotification> MapFromAsync(byte[] payload, EventId eventId, Guid referenceId, CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether the specified event type can be handled by the mapper
    /// </summary>
    /// <param name="eventType"></param>
    bool CanHandle(string eventType);
}
