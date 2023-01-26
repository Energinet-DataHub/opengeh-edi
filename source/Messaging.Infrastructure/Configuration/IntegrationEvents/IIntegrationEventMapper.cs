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

using MediatR;

namespace Messaging.Infrastructure.Configuration.IntegrationEvents;

/// <summary>
/// Maps from an integration event to a notification
/// </summary>
public interface IIntegrationEventMapper
{
    /// <summary>
    /// Map payload to a notification
    /// </summary>
    /// <param name="payload"></param>
    /// <returns><see cref="INotification"/></returns>
    INotification MapFrom(byte[] payload);

    /// <summary>
    /// Determines whether the specified event type can be handled by the mapper
    /// </summary>
    /// <param name="eventType"></param>
    bool CanHandle(string eventType);
}
