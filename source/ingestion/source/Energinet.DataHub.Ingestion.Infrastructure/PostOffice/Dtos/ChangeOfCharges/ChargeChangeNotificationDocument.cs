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
using System.Text.Json.Serialization;
using Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Dtos.Common;
using GreenEnergyHub.Messaging;
using GreenEnergyHub.Messaging.MessageTypes;
using GreenEnergyHub.Messaging.MessageTypes.Common;
using NodaTime;

namespace Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Dtos.ChangeOfCharges
{
    /// <summary>
    ///     Post Office document.
    ///     Partically defined by https://github.com/Energinet-DataHub/post-office/blob/main/source/Contracts/v1/Document.proto.
    ///     Transaction added to use green energy hub messaging
    /// </summary>
    [HubMessage("ChargeChangeNotification")]
    public class ChargeChangeNotificationDocument : IHubMessage
    {
        [JsonPropertyName("type")]
        public string Type => "chargechangenotification";

        [JsonPropertyName("effectuationDate")]
        public Instant EffectuationDate { get; set; }

        [JsonPropertyName("recipient")]
        public string? Recipient { get; set; }

        /// <summary>
        /// Data as serialized JSON.
        /// </summary>
        [JsonPropertyName("content")]
        public ChargeChangeNotificationContent? Content { get; set; }

        [JsonPropertyName("version")]
        public string Version => "1";

        /// <summary>
        /// The id of the hub message. Should be unique.
        /// </summary>
        public Transaction Transaction { get; set; } = Transaction.NewTransaction();
    }
}
