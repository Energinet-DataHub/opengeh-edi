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
using Energinet.DataHub.MarketRoles.Infrastructure.EDI;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Infrastructure.LocalMessageHub
{
    public class MessageHubMessage
    {
        public MessageHubMessage(string content, string correlation, DocumentType type, string recipient, Instant date, string gsrnNumber)
        {
            Id = Guid.NewGuid();
            Correlation = correlation;
            Type = type;
            Recipient = recipient;
            Date = date;
            GsrnNumber = gsrnNumber;
            Content = content;
        }

        public MessageHubMessage(Guid id, string messageContent, string correlation, DocumentType type, string recipient, Instant date, string gsrnNumber)
            : this(messageContent, correlation, type, recipient, date, gsrnNumber)
        {
            Id = id;
        }

        public Guid Id { get; }

        public string Content { get; }

        public string Correlation { get; }

        public DocumentType Type { get; }

        public string Recipient { get; }

        public Instant Date { get; }

        public string GsrnNumber { get; }

        public Instant? DequeuedDate { get; private set; }

        public string? BundleId { get; private set; }

        public void Dequeue(Instant date)
        {
            DequeuedDate = date;
        }

        public void AddToBundle(string bundleId)
        {
            BundleId = bundleId;
        }
    }
}
