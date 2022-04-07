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

namespace B2B.Transactions.IntegrationTests.TestDoubles
{
    public class OutgoingMessageStoreSpy : IOutgoingMessageStore
    {
        private readonly List<IMessage> _messages = new();

        public IReadOnlyCollection<IMessage> Messages => _messages.AsReadOnly();

        #pragma warning disable
        public Task<ReadOnlyCollection<IMessage>> GetUnpublishedAsync()
        {
            return Task.FromResult(_messages.AsReadOnly());
        }
        #pragma warning restore

        public void Add(IMessage message)
        {
            _messages.Add(message);
        }
    }
}
