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

using System.IO;
using System.Threading.Tasks;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.OutgoingMessages.Peek;

namespace Messaging.IntegrationTests.TestDoubles;

public class MessageStorageStub : MessageStorage
{
    private bool _shouldReturnEmptyMessage;

    public MessageStorageStub(IDatabaseConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public override Task<Stream?> GetMessageOfAsync(BundleId bundleId)
    {
        if (_shouldReturnEmptyMessage)
            return Task.FromResult(default(Stream));

        return base.GetMessageOfAsync(bundleId);
    }

    public void ReturnsEmptyMessage()
    {
        _shouldReturnEmptyMessage = true;
    }
}
