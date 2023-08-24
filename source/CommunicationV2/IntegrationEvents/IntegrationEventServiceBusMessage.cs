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

using System.Collections.ObjectModel;
using System.Text.Json;

namespace Energinet.DataHub.Core.Messaging.Communication;

public sealed class IntegrationEventServiceBusMessage
{
    internal IntegrationEventServiceBusMessage(Guid messageId, string subject, IReadOnlyDictionary<string, object> applicationProperties, BinaryData body)
    {
        MessageId = messageId;
        Subject = subject;
        ApplicationProperties = applicationProperties;
        Body = body;
    }

    public Guid MessageId { get; }

    public string Subject { get; }

    public IReadOnlyDictionary<string, object> ApplicationProperties { get; }

    public BinaryData Body { get; }

    public static IntegrationEventServiceBusMessage Create(byte[] message, IReadOnlyDictionary<string, object> bindingData)
    {
        var applicationPropertiesString = bindingData["ApplicationProperties"] as string ?? throw new InvalidOperationException("Subject is null");
        var applicationProperties = JsonSerializer.Deserialize<Dictionary<string, object>>(applicationPropertiesString) ?? throw new InvalidOperationException("Could not deserialize ApplicationProperties");
        return new IntegrationEventServiceBusMessage(
            Guid.Parse(bindingData["MessageId"] as string ?? throw new InvalidOperationException("MessageId is null")),
            bindingData["Subject"] as string ?? throw new InvalidOperationException("Subject is null"),
            new ReadOnlyDictionary<string, object>(applicationProperties),
            new BinaryData(message));
    }
}
