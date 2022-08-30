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
using Energinet.DataHub.MessageHub.Client.Storage;
using Messaging.Application.OutgoingMessages.Requesting;

namespace Messaging.Infrastructure.OutgoingMessages.Requesting;

public class MessageStorage : IMessageStorage
{
    private readonly IStorageHandler _storageHandler;
    private readonly MessageRequestContext _requestContext;

    public MessageStorage(IStorageHandler storageHandler, MessageRequestContext requestContext)
    {
        _storageHandler = storageHandler;
        _requestContext = requestContext;
    }

    public Task<Uri> SaveAsync(Stream bundledMessage, MessageRequest messageRequest)
    {
        if (_requestContext.DataBundleRequestDto is null)
        {
            throw new InvalidOperationException($"Data bundle request DTO is null");
        }

        return _storageHandler.AddStreamToStorageAsync(
            bundledMessage,
            _requestContext.DataBundleRequestDto);
    }
}
