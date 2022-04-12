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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using B2B.Transactions.OutgoingMessages;
using Energinet.DataHub.MessageHub.Model.Model;

namespace B2B.Transactions.IntegrationTests.TestDoubles
{
    public class DataAvailableNotificationPublisherSpy : IDataAvailableNotificationPublisher
    {
        private readonly List<DataAvailableNotificationDto> _publishedMessages = new();

        public ReadOnlyCollection<DataAvailableNotificationDto> PublishedMessages => _publishedMessages.AsReadOnly();

        public Task SendAsync(string correlationId, DataAvailableNotificationDto dataAvailableNotificationDto)
        {
            _publishedMessages.Add(dataAvailableNotificationDto);
            return Task.CompletedTask;
        }
    }
}
