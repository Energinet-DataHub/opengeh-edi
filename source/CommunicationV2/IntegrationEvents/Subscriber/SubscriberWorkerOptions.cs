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

namespace Energinet.DataHub.Core.Messaging.Communication.Subscriber;

/// <summary>
/// Settings for the communication with the Service Bus.
/// </summary>
public sealed class SubscriberWorkerOptions
{
    /// <summary>
    /// The connection string for the Service Bus.
    /// </summary>
    public string ServiceBusConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The name of the topic from where to receive integration events.
    /// </summary>
    public string TopicName { get; set; } = string.Empty;

    /// <summary>
    /// The name of the subscription.
    /// </summary>
    public string SubscriptionName { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of concurrent calls to the message handler.
    /// </summary>
    public int MaxConcurrentCalls { get; set; } = 3;
}
