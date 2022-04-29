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
using System.IO;
using System.Threading.Tasks;
using B2B.Transactions.OutgoingMessages;
using Energinet.DataHub.MessageHub.Model.Model;

namespace B2B.Transactions.IntegrationTests.TestDoubles
{
    public class MessageDispatcherSpy : IMessageDispatcher
    {
        public Stream? DispatchedMessage { get; private set; }

        public async Task<Uri> DispatchAsync(Stream message, DataBundleRequestDto requestDto)
        {
            if (requestDto == null) throw new ArgumentNullException(nameof(requestDto));
            DispatchedMessage = message;
            return await Task.FromResult(new Uri("https://randomUri.com")).ConfigureAwait(false);
        }
    }
}
